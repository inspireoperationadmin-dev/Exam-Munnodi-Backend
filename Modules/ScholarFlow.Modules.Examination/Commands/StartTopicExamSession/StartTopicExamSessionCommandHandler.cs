using Dapper;
using MediatR;
using ScholarFlow.Domain.Entities;
using ScholarFlow.Domain.Enums;
using ScholarFlow.Domain.Interfaces;
using ScholarFlow.Domain.Interfaces.Repositories;
using ScholarFlow.Modules.Academic.Public;
using ScholarFlow.Modules.Examination.DTOs;
using ScholarFlow.SharedKernel.Exceptions;

namespace ScholarFlow.Modules.Examination.Commands.StartTopicExamSession;

public sealed class StartTopicExamSessionCommandHandler(
    IExamSessionRepository examRepo,
    IExplanationRepository explanationRepo,
    ISqlConnectionFactory  sql,
    ICurrentUser           currentUser)
    : IRequestHandler<StartTopicExamSessionCommand, StartSessionResultDto>
{
    private const int RecentDays                  = 7;
    private const decimal NeedsRevisionRatio      = 0.60m;
    private const decimal UnvisitedQuestionRatio  = 0.30m;

    public async Task<StartSessionResultDto> Handle(StartTopicExamSessionCommand request, CancellationToken ct)
    {
        using var conn = sql.CreateConnection();

        // 1. Fetch SubjectId and all available questions belonging to this Topic
        var subjectId = await conn.QuerySingleOrDefaultAsync<Guid?>("""
            SELECT SubjectId FROM Topics WHERE Id = @TopicId AND IsDeleted = 0
            """, new { request.TopicId });

        if (!subjectId.HasValue)
            throw new NotFoundException("Topic not found.");

        var questionPool = (await conn.QueryAsync<QuestionPoolItem>("""
            SELECT
                q.Id AS QuestionId,
                q.SubTopicId,
                st.TopicId,
                t.SubjectId,
                q.ManualDifficulty,
                q.SystemDifficulty
            FROM Questions q
            INNER JOIN SubTopics st ON st.Id = q.SubTopicId
            INNER JOIN Topics t ON t.Id = st.TopicId
            WHERE st.TopicId = @TopicId AND q.IsDeleted = 0
            """, new { request.TopicId })).ToList();

        if (questionPool.Count == 0)
            throw new BadRequestException("No questions available for this topic.");

        if (request.Mode == ExamMode.TopicExam && questionPool.Count < request.Limit)
            throw new BadRequestException("This topic doesn't have enough questions currently. Please try another topic.");

        var questionIds = questionPool.Select(q => q.QuestionId).ToList();
        var progressRows = (await conn.QueryAsync<TopicQuestionProgressRow>("""
            SELECT
                QuestionId,
                TimesAttempted,
                LastAnswerCorrect,
                Status,
                LastSeenAt
            FROM StudentTopicQuestionProgresses
            WHERE UserId = @UserId AND QuestionId IN @QuestionIds
            """, new
            {
                currentUser.UserId,
                QuestionIds = questionIds
            })).ToList();

        // 2. Pick topic questions adaptively for TopicExam; keep TopicPractice simple/random.
        var rng = new Random();
        var selectedIds = request.Mode == ExamMode.TopicExam
            ? SelectAdaptiveTopicQuestions(questionPool, progressRows, request.Limit, rng)
            : questionIds
                .OrderBy(_ => rng.Next())
                .Take(request.Limit)
                .ToList();

        if (request.Mode == ExamMode.TopicExam && selectedIds.Count < request.Limit)
            throw new BadRequestException("This topic doesn't have enough questions currently. Please try another topic.");

        // 3. Create dynamic topic exam session (PaperId = null)
        var session = ExamSession.Start(
            userId:         currentUser.UserId,
            paperId:        null, 
            subjectId:      subjectId.Value,
            mode:           request.Mode,
            timeLimitMinutes: request.Mode == ExamMode.TopicPractice ? null : selectedIds.Count);

        await examRepo.AddAsync(session, ct);

        // 4. Pre-populate UserResponse & ExamSessionQuestion with sequential layout OrderIndexes
        for (int i = 0; i < selectedIds.Count; i++)
        {
            var qId = selectedIds[i];

            await examRepo.AddResponseAsync(new UserResponse
            {
                Id             = Guid.NewGuid(),
                SessionId      = session.Id,
                QuestionId     = qId,
                OrderIndex     = i + 1, 
                ResponseStatus = ResponseStatus.Unvisited
            }, ct);

            await examRepo.AddSessionQuestionAsync(new ExamSessionQuestion
            {
                Id         = Guid.NewGuid(),
                SessionId  = session.Id,
                QuestionId = qId,
                OrderIndex = i + 1
            }, ct);
        }

        await examRepo.SaveChangesAsync(ct);

        // 5. Query detailed question texts and option lists using Dapper (Added ORDER BY)
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
                        o.OptionImageUrl,
                        o.IsCorrect     AS OptionIsCorrect
                    FROM Questions q
                    LEFT JOIN Options o ON o.QuestionId = q.Id
                    WHERE q.Id IN @QuestionIds
                    AND q.IsDeleted  = 0
                    ORDER BY o.Label -- <-- Corrected T-SQL comment style [1]
                    """,
            new { QuestionIds = selectedIds });

        var questionsDict = new Dictionary<Guid, (int Order, string Text, string? Image, decimal Marks, List<ExamOptionDto> Options, Guid? CorrectOptionId)>();

        foreach (var row in rows)
        {
            if (!questionsDict.TryGetValue(row.QuestionId, out var q))
            {
                q = (row.OrderIndex, row.QuestionText, row.QuestionImageUrl, row.Marks, [], null);
                questionsDict[row.QuestionId] = q;
            }

            if (row.OptionId.HasValue)
            {
                q.Options.Add(new ExamOptionDto(
                    Id:             row.OptionId.Value,
                    Label:          row.Label ?? string.Empty,
                    OptionText:     row.OptionText ?? string.Empty,
                    OptionImageUrl: row.OptionImageUrl));

                if (row.OptionIsCorrect)
                {
                    questionsDict[row.QuestionId] = (q.Order, q.Text, q.Image, q.Marks, q.Options, row.OptionId.Value);
                }
            }
        }

        // 6. If TopicPractice mode, retrieve all explanation sections in a single bulk query
        var explanationsMap = new Dictionary<Guid, Explanation>();
        if (request.Mode == ExamMode.TopicPractice)
        {
            var explanationsList = await explanationRepo.GetByQuestionIdsAsync(selectedIds, ct);
            explanationsMap = explanationsList.ToDictionary(e => e.QuestionId);
        }

        // 7. Map final DTO list following the exact randomized shuffled sequence and sort options
        var examQuestions = selectedIds.Select((id, index) =>
        {
            if (!questionsDict.TryGetValue(id, out var details))
            {
                return null;
            }

            Guid? correctOptionId = null;
            string? explanationText = null;

            if (request.Mode == ExamMode.TopicPractice)
            {
                correctOptionId = details.CorrectOptionId;

                // Returns a clean, non-LaTeX string with the title on the first line [1]
                if (explanationsMap.TryGetValue(id, out var explanation) && explanation.Sections.Any())
                {
                    explanationText = string.Join("\n\n", explanation.Sections
                        .OrderBy(s => s.OrderIndex)
                        .Select(s => $"{s.Title}\n{s.Content}"));
                }
            }

            return new ExamQuestionDto(
                Id:               id,
                OrderIndex:       index + 1,
                QuestionText:     details.Text,
                QuestionImageUrl: details.Image,
                Marks:            details.Marks,
                Options:          details.Options.OrderBy(o => o.Label).ToList(), // <-- Explicit memory sort
                CorrectOptionId:  correctOptionId,
                explanationText
            );
        })
        .Where(q => q != null)
        .Cast<ExamQuestionDto>()
        .ToList();

        return new StartSessionResultDto(
            SessionId:     session.Id,
            StartTime:     session.StartTime,
            ServerNow:     DateTime.UtcNow,
            ExpiresAt:     session.ExpiresAt,
            TimeLimitMinutes: session.TimeLimitMinutes,
            Mode:          session.Mode.ToString(),
            QuestionCount: selectedIds.Count,
            Questions:     examQuestions);
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
        public bool OptionIsCorrect { get; set; }
    }

    private sealed class TopicQuestionProgressRow
    {
        public Guid QuestionId { get; set; }
        public int TimesAttempted { get; set; }
        public bool LastAnswerCorrect { get; set; }
        public QuestionProgressStatus Status { get; set; }
        public DateTime LastSeenAt { get; set; }
    }

    private static List<Guid> SelectAdaptiveTopicQuestions(
        IReadOnlyList<QuestionPoolItem> pool,
        IReadOnlyList<TopicQuestionProgressRow> progressRows,
        int totalQuestions,
        Random rng)
    {
        var progressByQuestionId = progressRows.ToDictionary(p => p.QuestionId);
        var recentCutoff = DateTime.UtcNow.AddDays(-RecentDays);
        var recentQuestionIds = progressRows
            .Where(p => p.LastSeenAt >= recentCutoff)
            .Select(p => p.QuestionId)
            .ToHashSet();

        var needsRevisionQuestions = pool
            .Where(q => progressByQuestionId.TryGetValue(q.QuestionId, out var progress)
                     && progress.TimesAttempted > 0
                     && (progress.Status == QuestionProgressStatus.NeedsRevision
                      || !progress.LastAnswerCorrect))
            .ToList();

        var unvisitedQuestions = pool
            .Where(q => !progressByQuestionId.ContainsKey(q.QuestionId))
            .ToList();

        var masteredQuestions = pool
            .Where(q => progressByQuestionId.TryGetValue(q.QuestionId, out var progress)
                     && progress.TimesAttempted > 0
                     && progress.Status == QuestionProgressStatus.Mastered
                     && progress.LastAnswerCorrect)
            .ToList();

        var needsRevisionTarget = (int)Math.Round(totalQuestions * NeedsRevisionRatio, MidpointRounding.AwayFromZero);
        var unvisitedTarget = (int)Math.Round(totalQuestions * UnvisitedQuestionRatio, MidpointRounding.AwayFromZero);
        var masteredTarget = Math.Max(0, totalQuestions - needsRevisionTarget - unvisitedTarget);

        var selected = new List<Guid>(totalQuestions);

        PickDiverse(needsRevisionQuestions, selected, needsRevisionTarget, recentQuestionIds, rng);
        PickDiverse(unvisitedQuestions, selected, unvisitedTarget, recentQuestionIds, rng);
        PickDiverse(masteredQuestions, selected, masteredTarget, recentQuestionIds, rng);

        if (selected.Count < totalQuestions)
        {
            FillRemaining([needsRevisionQuestions, unvisitedQuestions, masteredQuestions], selected, totalQuestions, recentQuestionIds, rng);
        }

        if (selected.Count < totalQuestions)
        {
            FillRemaining([needsRevisionQuestions, unvisitedQuestions, masteredQuestions], selected, totalQuestions, new HashSet<Guid>(), rng);
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

        var subTopicQueues = candidates
            .GroupBy(q => q.SubTopicId)
            .OrderBy(_ => rng.Next())
            .Select(group => new Queue<QuestionPoolItem>(group
                .OrderBy(q => q.EffectiveDifficulty)
                .ThenBy(_ => rng.Next())))
            .ToList();

        while (selected.Count < targetCount && subTopicQueues.Count > 0)
        {
            foreach (var queue in subTopicQueues.ToList())
            {
                if (selected.Count >= targetCount)
                    break;

                if (queue.TryDequeue(out var question))
                {
                    selected.Add(question.QuestionId);
                }

                if (queue.Count == 0)
                    subTopicQueues.Remove(queue);
            }
        }
    }
}
