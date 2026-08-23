using Dapper;
using MediatR;
using ScholarFlow.Domain.Entities;
using ScholarFlow.Domain.Enums;
using ScholarFlow.Domain.Interfaces;
using ScholarFlow.Domain.Interfaces.Repositories;
using ScholarFlow.Modules.Academic.Public;
using ScholarFlow.Modules.Examination.DTOs;
using ScholarFlow.Modules.Examination.Services;
using ScholarFlow.SharedKernel.Exceptions;

namespace ScholarFlow.Modules.Examination.Commands.GeneratePersonalizedExam;

public sealed class GeneratePersonalizedExamCommandHandler(
    IAcademicApi           academicApi,
    IAnalyticsApi          analyticsApi,
    ISqlConnectionFactory  sql,
    ISubscriptionsApi      subscriptionsApi,
    IExamSessionStartPolicy sessionStartPolicy,
    ICurrentUser           currentUser)
    : IRequestHandler<GeneratePersonalizedExamCommand, StartSessionResultDto>
{
    private const int RecentDays                  = 7;
    private const decimal NeedsPracticeRatio      = 0.60m;
    private const decimal UnvisitedQuestionRatio = 0.30m;

    public async Task<StartSessionResultDto> Handle(GeneratePersonalizedExamCommand request, CancellationToken ct)
    {
        await subscriptionsApi.EnsureActiveAccessAsync(currentUser.UserId, ct);

        // These module APIs share the request-scoped DbContext, so keep the EF calls sequential.
        var pool = await academicApi.GetQuestionPoolAsync(request.SubjectId, ct);
        var recentQuestionIds = await analyticsApi.GetRecentlySeenQuestionIdsAsync(currentUser.UserId, RecentDays, ct);
        var totalQuestions = request.QuestionCount;

        if (pool.Count == 0)
            throw new BadRequestException("No questions available for this subject.");

        if (pool.Count < totalQuestions)
            throw new BadRequestException("This subject doesn't have enough questions for a mock exam currently. Please try another subject or paper.");

        var poolQuestionIds = pool.Select(q => q.QuestionId).ToList();
        var progressSummaries = await analyticsApi.GetQuestionProgressSummariesAsync(currentUser.UserId, poolQuestionIds, ct);

        var mockExamTimeLimitMinutes = ExamTimeLimitCalculator.CalculateRealPaperPacedMinutes(totalQuestions);

        // 2. Build adaptive mock exam mix from the student's per-question history.
        var rng = new Random();
        var selected = SelectAdaptiveMockQuestions(
            pool,
            progressSummaries,
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

        await sessionStartPolicy.EnsureCanStartAsync(session, request.ReplaceSessionId, ct);

        var usageToken = await subscriptionsApi.ConsumeExamUsageAsync(
            currentUser.UserId,
            SubscriptionUsageFeature.MockExam,
            ct);

        try
        {
            // 10. Pre-populate UserResponse with direct OrderIndex mapping
            var responses = new List<UserResponse>(selected.Count);
            var sessionQuestions = new List<ExamSessionQuestion>(selected.Count);
            for (int i = 0; i < selected.Count; i++)
            {
                responses.Add(new UserResponse
                {
                    Id             = Guid.NewGuid(),
                    SessionId      = session.Id,
                    QuestionId     = selected[i],
                    OrderIndex     = i + 1,
                    ResponseStatus = ResponseStatus.Unvisited
                });

                sessionQuestions.Add(new ExamSessionQuestion
                {
                    Id         = Guid.NewGuid(),
                    SessionId  = session.Id,
                    QuestionId = selected[i],
                    OrderIndex = i + 1
                });
            }

            await sessionStartPolicy.PersistAsync(
                session,
                responses,
                sessionQuestions,
                request.ReplaceSessionId,
                ct);
        }
        catch
        {
            if (usageToken is not null)
            {
                await subscriptionsApi.ReleaseExamUsageAsync(
                    currentUser.UserId,
                    usageToken,
                    CancellationToken.None);
            }

            throw;
        }

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
            INNER JOIN Papers p ON p.Id = q.PaperId
            LEFT JOIN Options o ON o.QuestionId = q.Id
            WHERE q.Id IN @QuestionIds
              AND q.IsDeleted  = 0
              AND p.IsDeleted = 0
              AND p.IsPublic = 1
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
        IReadOnlyList<QuestionProgressSummary> progressSummaries,
        HashSet<Guid> recentQuestionIds,
        int totalQuestions,
        Random rng)
    {
        var progressByQuestionId = progressSummaries.ToDictionary(h => h.QuestionId);

        var needsPracticeQuestions = pool
            .Where(q => progressByQuestionId.TryGetValue(q.QuestionId, out var history)
                     && NeedsPractice(history))
            .ToList();

        var unvisitedQuestions = pool
            .Where(q => !progressByQuestionId.ContainsKey(q.QuestionId))
            .ToList();

        var masteredQuestions = pool
            .Where(q => progressByQuestionId.TryGetValue(q.QuestionId, out var history)
                     && history.TimesAttempted > 0
                     && history.LastResponseWasAnswered
                     && history.LastAnswerCorrect
                     && history.MasteryScore >= 100)
            .ToList();

        var needsPracticeTarget = (int)Math.Round(totalQuestions * NeedsPracticeRatio, MidpointRounding.AwayFromZero);
        var unvisitedTarget = (int)Math.Round(totalQuestions * UnvisitedQuestionRatio, MidpointRounding.AwayFromZero);
        var masteredTarget = Math.Max(0, totalQuestions - needsPracticeTarget - unvisitedTarget);

        var selected = new List<Guid>(totalQuestions);

        PickDiverse(needsPracticeQuestions, selected, needsPracticeTarget, [], rng, progressByQuestionId);
        PickDiverse(unvisitedQuestions, selected, unvisitedTarget, recentQuestionIds, rng, progressByQuestionId);
        PickDiverse(masteredQuestions, selected, masteredTarget, recentQuestionIds, rng, progressByQuestionId);

        if (selected.Count < totalQuestions)
        {
            FillRemaining([needsPracticeQuestions, unvisitedQuestions, masteredQuestions, pool], selected, totalQuestions, recentQuestionIds, rng, progressByQuestionId);
        }

        if (selected.Count < totalQuestions)
        {
            FillRemaining([needsPracticeQuestions, unvisitedQuestions, masteredQuestions, pool], selected, totalQuestions, [], rng, progressByQuestionId);
        }

        return selected
            .Distinct()
            .Take(totalQuestions)
            .ToList();
    }

    private static bool NeedsPractice(QuestionProgressSummary progress)
        => !progress.LastResponseWasAnswered
        || progress.TimesAttempted > 0
            && (!progress.LastAnswerCorrect || progress.MasteryScore < 100);

    private static void FillRemaining(
        IReadOnlyList<IReadOnlyList<QuestionPoolItem>> buckets,
        List<Guid> selected,
        int totalQuestions,
        HashSet<Guid> recentQuestionIds,
        Random rng,
        IReadOnlyDictionary<Guid, QuestionProgressSummary> progressByQuestionId)
    {
        foreach (var bucket in buckets)
        {
            if (selected.Count >= totalQuestions) return;
            PickDiverse(bucket, selected, totalQuestions - selected.Count, recentQuestionIds, rng, progressByQuestionId);
        }
    }

    private static void PickDiverse(
        IReadOnlyList<QuestionPoolItem> source,
        List<Guid> selected,
        int needed,
        HashSet<Guid> recentQuestionIds,
        Random rng,
        IReadOnlyDictionary<Guid, QuestionProgressSummary> progressByQuestionId)
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
                .OrderBy(q => SelectionPriority(q, progressByQuestionId))
                .ThenBy(q => q.EffectiveDifficulty)
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

    private static decimal SelectionPriority(
        QuestionPoolItem question,
        IReadOnlyDictionary<Guid, QuestionProgressSummary> progressByQuestionId)
    {
        if (!progressByQuestionId.TryGetValue(question.QuestionId, out var progress))
            return 50;

        if (!progress.LastResponseWasAnswered)
            return 10;

        if (progress.TimesAttempted > 0 && !progress.LastAnswerCorrect)
            return 0;

        return progress.MasteryScore;
    }

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
