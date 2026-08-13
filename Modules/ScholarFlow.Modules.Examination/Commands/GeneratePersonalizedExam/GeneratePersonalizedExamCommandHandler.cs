using Dapper;
using MediatR;
using ScholarFlow.Domain.Entities;
using ScholarFlow.Domain.Enums;
using ScholarFlow.Domain.Interfaces;
using ScholarFlow.Domain.Interfaces.Repositories;
using ScholarFlow.Modules.Academic.Public;
using ScholarFlow.Modules.Examination.DTOs;
using ScholarFlow.SharedKernel.Exceptions;

namespace ScholarFlow.Modules.Examination.Commands.GeneratePersonalizedExam;

public sealed class GeneratePersonalizedExamCommandHandler(
    IExamSessionRepository examRepo,
    IAcademicApi           academicApi,
    IAnalyticsApi          analyticsApi,
    ISqlConnectionFactory  sql,
    ICurrentUser           currentUser)
    : IRequestHandler<GeneratePersonalizedExamCommand, StartSessionResultDto>
{
    private const int RecentDays            = 7;
    private const decimal WrongQuestionRatio     = 0.60m;
    private const decimal UnvisitedQuestionRatio = 0.30m;

    public async Task<StartSessionResultDto> Handle(GeneratePersonalizedExamCommand request, CancellationToken ct)
    {
        // These module APIs share the request-scoped DbContext, so keep the EF calls sequential.
        var pool = await academicApi.GetQuestionPoolAsync(request.SubjectId, ct);
        var recentQuestionIds = await analyticsApi.GetRecentlySeenQuestionIdsAsync(currentUser.UserId, RecentDays, ct);
        var totalQuestions = request.QuestionCount;

        if (pool.Count == 0)
            throw new BadRequestException("No questions available for this subject.");

        if (pool.Count < totalQuestions)
            throw new BadRequestException("This subject doesn't have enough questions for a mock exam currently. Please try another subject or paper.");

        var poolQuestionIds = pool.Select(q => q.QuestionId).ToList();
        var histories = await analyticsApi.GetQuestionHistoriesAsync(currentUser.UserId, poolQuestionIds, ct);

        var mockExamTimeLimitMinutes = CalculateMockTimeLimitMinutes(totalQuestions);

        // 2. Build adaptive mock exam mix from the student's per-question history.
        var rng = new Random();
        var selected = SelectAdaptiveMockQuestions(
            pool,
            histories,
            recentQuestionIds,
            totalQuestions,
            rng);

        if (selected.Count < totalQuestions)
            throw new BadRequestException("This subject doesn't have enough questions for a mock exam currently. Please try another subject or paper.");


        // 8. Shuffle final list
        selected = [.. selected.OrderBy(_ => rng.Next())];

        // 9. Create mock exam session (PaperId = null, Mode = MockExam)
        var session = ExamSession.Start(
            userId:         currentUser.UserId,
            paperId:        null,
            subjectId:      request.SubjectId,
            mode:           ExamMode.MockExam,
            timeLimitMinutes: mockExamTimeLimitMinutes);

        await examRepo.AddAsync(session, ct);

        // 10. Pre-populate UserResponse with direct OrderIndex mapping
        for (int i = 0; i < selected.Count; i++)
        {
            await examRepo.AddResponseAsync(new UserResponse
            {
                Id             = Guid.NewGuid(),
                SessionId      = session.Id,
                QuestionId     = selected[i],
                OrderIndex     = i + 1, 
                ResponseStatus = ResponseStatus.Unvisited
            }, ct);

            await examRepo.AddSessionQuestionAsync(new ExamSessionQuestion
            {
                Id         = Guid.NewGuid(),
                SessionId  = session.Id,
                QuestionId = selected[i],
                OrderIndex = i + 1
            }, ct);
        }

        await examRepo.SaveChangesAsync(ct);

        // 11. Fetch the shuffled question texts and option details using Dapper (Added ORDER BY) [1]
        using var conn = sql.CreateConnection();

        var rows = await conn.QueryAsync<QuestionRow>("""
            SELECT
                q.Id            AS QuestionId,
                q.OrderIndex,
                q.QuestionText,
                q.Marks,
                q.QuestionImageUrl,
                o.Id            AS OptionId,
                o.Label,
                o.OptionText,
                o.OptionImageUrl
            FROM Questions q
            LEFT JOIN Options o ON o.QuestionId = q.Id
            WHERE q.Id IN @QuestionIds
              AND q.IsDeleted  = 0
            ORDER BY o.Label
            """,
            new { QuestionIds = selected });

        var questionsDict = new Dictionary<Guid, (int Order, string Text, string? Image, decimal Marks, List<ExamOptionDto> Options)>();

        foreach (var row in rows)
        {
            if (!questionsDict.TryGetValue(row.QuestionId, out var q))
            {
                q = (row.OrderIndex, row.QuestionText, row.QuestionImageUrl, row.Marks, []);
                questionsDict[row.QuestionId] = q;
            }

            if (row.OptionId.HasValue)
            {
                q.Options.Add(new ExamOptionDto(
                    Id:             row.OptionId.Value,
                    Label:          row.Label ?? string.Empty,
                    OptionText:     row.OptionText ?? string.Empty,
                    OptionImageUrl: row.OptionImageUrl));
            }
        }

        // 12. Map results following the exact shuffled order in the 'selected' list and sort options [1]
        var examQuestions = selected.Select((id, index) =>
        {
            if (!questionsDict.TryGetValue(id, out var details))
            {
                return null;
            }

            return new ExamQuestionDto(
                Id:               id,
                OrderIndex:       index + 1,
                QuestionText:     details.Text,
                QuestionImageUrl: details.Image,
                Marks:            details.Marks,
                Options:          details.Options.OrderBy(o => o.Label).ToList(), // <-- Explicit memory sort [1]
                CorrectOptionId:  null, 
                ExplanationText:  null  
            );
        })
        .Where(q => q != null)
        .Cast<ExamQuestionDto>()
        .ToList();

        // 13. Return atomic result containing the generated layout
        return new StartSessionResultDto(
            SessionId:     session.Id,
            StartTime:     session.StartTime,
            ServerNow:     DateTime.UtcNow,
            ExpiresAt:     session.ExpiresAt,
            TimeLimitMinutes: session.TimeLimitMinutes,
            Mode:          session.Mode.ToString(),
            QuestionCount: selected.Count,
            Questions:     examQuestions);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static List<Guid> SelectAdaptiveMockQuestions(
        IReadOnlyList<QuestionPoolItem> pool,
        IReadOnlyList<QuestionHistorySummary> histories,
        HashSet<Guid> recentQuestionIds,
        int totalQuestions,
        Random rng)
    {
        var historyByQuestionId = histories.ToDictionary(h => h.QuestionId);

        var wrongQuestions = pool
            .Where(q => historyByQuestionId.TryGetValue(q.QuestionId, out var history)
                     && history.TimesAttempted > 0
                     && history.LastResponseWasAnswered
                     && !history.LastAnswerCorrect)
            .ToList();

        var skippedQuestions = pool
            .Where(q => historyByQuestionId.TryGetValue(q.QuestionId, out var history)
                     && history.TimesAttempted > 0
                     && !history.LastResponseWasAnswered)
            .ToList();

        var unvisitedQuestions = pool
            .Where(q => !historyByQuestionId.ContainsKey(q.QuestionId))
            .ToList();

        var correctQuestions = pool
            .Where(q => historyByQuestionId.TryGetValue(q.QuestionId, out var history)
                     && history.TimesAttempted > 0
                     && history.LastAnswerCorrect)
            .ToList();

        var wrongTarget = (int)Math.Round(totalQuestions * WrongQuestionRatio, MidpointRounding.AwayFromZero);
        var unvisitedTarget = (int)Math.Round(totalQuestions * UnvisitedQuestionRatio, MidpointRounding.AwayFromZero);
        var correctTarget = Math.Max(0, totalQuestions - wrongTarget - unvisitedTarget);

        var selected = new List<Guid>(totalQuestions);

        PickDiverse(wrongQuestions, selected, wrongTarget, recentQuestionIds, rng);
        PickDiverse(unvisitedQuestions, selected, unvisitedTarget, recentQuestionIds, rng);
        PickDiverse(correctQuestions, selected, correctTarget, recentQuestionIds, rng);

        if (selected.Count < totalQuestions)
        {
            FillRemaining([wrongQuestions, skippedQuestions, unvisitedQuestions, correctQuestions], selected, totalQuestions, recentQuestionIds, rng);
        }

        if (selected.Count < totalQuestions)
        {
            FillRemaining([wrongQuestions, skippedQuestions, unvisitedQuestions, correctQuestions], selected, totalQuestions, new HashSet<Guid>(), rng);
        }

        return selected
            .Distinct()
            .Take(totalQuestions)
            .ToList();
    }

    private static void FillRemaining(
        IReadOnlyList<IReadOnlyList<QuestionPoolItem>> buckets,
        List<Guid> selected,
        int totalQuestions,
        HashSet<Guid> recentQuestionIds,
        Random rng)
    {
        foreach (var bucket in buckets)
        {
            if (selected.Count >= totalQuestions) return;
            PickDiverse(bucket, selected, totalQuestions - selected.Count, recentQuestionIds, rng);
        }
    }

    private static void PickDiverse(
        IReadOnlyList<QuestionPoolItem> source,
        List<Guid> selected,
        int needed,
        HashSet<Guid> recentQuestionIds,
        Random rng)
    {
        if (needed <= 0 || source.Count == 0) return;

        var selectedSet = selected.ToHashSet();
        var targetCount = selected.Count + needed;
        var candidates = source
            .Where(q => !selectedSet.Contains(q.QuestionId)
                     && !recentQuestionIds.Contains(q.QuestionId))
            .ToList();

        if (candidates.Count == 0)
        {
            candidates = source
                .Where(q => !selectedSet.Contains(q.QuestionId))
                .ToList();
        }

        var topicQueues = candidates
            .GroupBy(q => q.TopicId)
            .OrderBy(_ => rng.Next())
            .Select(group => new Queue<QuestionPoolItem>(group
                .OrderBy(q => q.EffectiveDifficulty)
                .ThenBy(_ => rng.Next())))
            .ToList();

        while (selected.Count < targetCount && topicQueues.Count > 0)
        {
            foreach (var queue in topicQueues.ToList())
            {
                if (selected.Count >= targetCount)
                    break;

                if (queue.TryDequeue(out var question))
                {
                    selected.Add(question.QuestionId);
                }

                if (queue.Count == 0)
                    topicQueues.Remove(queue);
            }
        }
    }

    private static int CalculateMockTimeLimitMinutes(int questionCount)
        => (int)Math.Ceiling(questionCount * 2.4);

    private sealed class QuestionRow
    {
        public Guid QuestionId { get; set; }
        public int OrderIndex { get; set; }
        public string QuestionText { get; set; } = string.Empty;
        public decimal Marks { get; set; }
        public string? QuestionImageUrl { get; set; }
        public Guid? OptionId { get; set; }
        public string? Label { get; set; }
        public string? OptionText { get; set; }
        public string? OptionImageUrl { get; set; }
    }
}
