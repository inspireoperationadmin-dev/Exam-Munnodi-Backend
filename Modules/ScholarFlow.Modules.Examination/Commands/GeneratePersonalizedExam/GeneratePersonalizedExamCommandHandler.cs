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
    private const int MinAttemptsForTier    = 1;

    public async Task<StartSessionResultDto> Handle(GeneratePersonalizedExamCommand request, CancellationToken ct)
    {
        // These module APIs share the request-scoped DbContext, so keep the EF calls sequential.
        var pool = await academicApi.GetQuestionPoolAsync(request.SubjectId, ct);
        var performances = await analyticsApi.GetSubTopicPerformancesAsync(currentUser.UserId, request.SubjectId, ct);
        var recentQuestionIds = await analyticsApi.GetRecentlySeenQuestionIdsAsync(currentUser.UserId, RecentDays, ct);

        if (pool.Count == 0)
            throw new BadRequestException("No questions available for this subject.");

        var totalQuestions = request.QuestionCount;
        var mockExamTimeLimitMinutes = CalculateMockTimeLimitMinutes(totalQuestions);

        // 2. Classify subtopics into weakness tiers
        var perfBySubTopic = performances.ToDictionary(p => p.SubTopicId);
        var subTopicTiers  = ClassifySubTopicTiers(pool, perfBySubTopic);
        var hasMeasuredProgress = performances.Any(p => p.TotalAttempts >= MinAttemptsForTier);

        // 3. Target question counts per tier
        var easyCount   = (int)Math.Round(totalQuestions * 0.40);
        var weakCount   = (int)Math.Round(totalQuestions * 0.30);
        var avgCount    = (int)Math.Round(totalQuestions * 0.20);
        var unknownCount = totalQuestions - easyCount - weakCount - avgCount;

        var targets = new Dictionary<WeaknessTier, int>
        {
            [WeaknessTier.VeryWeak] = easyCount,
            [WeaknessTier.Weak]     = weakCount,
            [WeaknessTier.Average]  = avgCount,
            [WeaknessTier.Unknown]  = unknownCount
        };

        // 4. Difficulty mix per tier
        var difficultyMix = new Dictionary<WeaknessTier, (double Easy, double Medium, double Hard)>
        {
            [WeaknessTier.VeryWeak] = (0.40, 0.40, 0.20),
            [WeaknessTier.Weak]     = (0.20, 0.50, 0.30),
            [WeaknessTier.Average]  = (0.10, 0.40, 0.50),
            [WeaknessTier.Unknown]  = (0.50, 0.50, 0.00)
        };

        // 5. Build pool index: (tier, difficulty) → list of questionIds
        var poolIndex = BuildPoolIndex(pool, subTopicTiers, recentQuestionIds);

        // 6. Select questions
        var selected = new List<Guid>();
        var rng      = new Random();

        if (hasMeasuredProgress)
        {
            foreach (var (tier, totalCount) in targets)
            {
                if (totalCount == 0) continue;

                var mix = difficultyMix[tier];
                int easyNeed   = (int)Math.Round(totalCount * mix.Easy);
                int mediumNeed = (int)Math.Round(totalCount * mix.Medium);
                int hardNeed   = totalCount - easyNeed - mediumNeed;

                PickFromPool(poolIndex, selected, tier, SystemDifficultyLevel.Easy,   easyNeed,   pool, subTopicTiers, rng);
                PickFromPool(poolIndex, selected, tier, SystemDifficultyLevel.Medium, mediumNeed, pool, subTopicTiers, rng);
                PickFromPool(poolIndex, selected, tier, SystemDifficultyLevel.Hard,   hardNeed,   pool, subTopicTiers, rng);
            }
        }
        else
        {
            selected.AddRange(SelectBalancedDiagnosticQuestions(pool, recentQuestionIds, totalQuestions, rng));
        }

        // 7. Final fallback — fill remaining from any unseen questions
        if (selected.Count < totalQuestions)
        {
            var alreadySelected = selected.ToHashSet();
            var extras = pool
                .Where(q => !alreadySelected.Contains(q.QuestionId))
                .OrderBy(_ => rng.Next())
                .Take(totalQuestions - selected.Count)
                .Select(q => q.QuestionId);
            selected.AddRange(extras);
        }

        selected = [.. selected.Take(totalQuestions)];

        if (selected.Count == 0)
            throw new BadRequestException("Could not generate exam — insufficient questions for this subject.");

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

    private static Dictionary<Guid, WeaknessTier> ClassifySubTopicTiers(
        IReadOnlyList<QuestionPoolItem> pool,
        Dictionary<Guid, SubTopicPerformanceSummary> perfBySubTopic)
    {
        var result = new Dictionary<Guid, WeaknessTier>();

        foreach (var subTopicId in pool.Select(q => q.SubTopicId).Distinct())
        {
            if (!perfBySubTopic.TryGetValue(subTopicId, out var perf)
                || perf.TotalAttempts < MinAttemptsForTier)
            {
                result[subTopicId] = WeaknessTier.Unknown;
            }
            else
            {
                result[subTopicId] = perf.CorrectPercentage switch
                {
                    < 40  => WeaknessTier.VeryWeak,
                    < 60  => WeaknessTier.Weak,
                    _     => WeaknessTier.Average
                };
            }
        }

        return result;
    }

    private static Dictionary<(WeaknessTier, SystemDifficultyLevel), List<Guid>> BuildPoolIndex(
        IReadOnlyList<QuestionPoolItem> pool,
        Dictionary<Guid, WeaknessTier> subTopicTiers,
        HashSet<Guid> recentQuestionIds)
    {
        var index = new Dictionary<(WeaknessTier, SystemDifficultyLevel), List<Guid>>();

        foreach (var q in pool)
        {
            if (recentQuestionIds.Contains(q.QuestionId)) continue;

            var tier       = subTopicTiers.GetValueOrDefault(q.SubTopicId, WeaknessTier.Unknown);
            var difficulty = q.EffectiveDifficulty;
            var key        = (tier, difficulty);

            if (!index.TryGetValue(key, out var list))
            {
                list = [];
                index[key] = list;
            }
            list.Add(q.QuestionId);
        }

        return index;
    }

    private static void PickFromPool(
        Dictionary<(WeaknessTier, SystemDifficultyLevel), List<Guid>> poolIndex,
        List<Guid> selected,
        WeaknessTier tier,
        SystemDifficultyLevel difficulty,
        int needed,
        IReadOnlyList<QuestionPoolItem> pool,
        Dictionary<Guid, WeaknessTier> subTopicTiers,
        Random rng)
    {
        if (needed <= 0) return;

        var selectedSet = selected.ToHashSet();

        // Primary: exact tier + difficulty match
        var candidates = poolIndex
            .GetValueOrDefault((tier, difficulty), [])
            .Where(id => !selectedSet.Contains(id))
            .OrderBy(_ => rng.Next())
            .Take(needed)
            .ToList();

        selected.AddRange(candidates);
        needed -= candidates.Count;
        if (needed <= 0) return;

        // Fallback 1: same tier, any difficulty
        selectedSet = selected.ToHashSet();
        var tierFallback = pool
            .Where(q => !selectedSet.Contains(q.QuestionId)
                     && subTopicTiers.GetValueOrDefault(q.SubTopicId, WeaknessTier.Unknown) == tier)
            .OrderBy(_ => rng.Next())
            .Take(needed)
            .Select(q => q.QuestionId)
            .ToList();

        selected.AddRange(tierFallback);
        needed -= tierFallback.Count;
        if (needed <= 0) return;

        // Fallback 2: same difficulty, any tier (include recently seen)
        selectedSet = selected.ToHashSet();
        var diffFallback = pool
            .Where(q => !selectedSet.Contains(q.QuestionId)
                     && q.EffectiveDifficulty == difficulty)
            .OrderBy(_ => rng.Next())
            .Take(needed)
            .Select(q => q.QuestionId)
            .ToList();

        selected.AddRange(diffFallback);
    }

    private static List<Guid> SelectBalancedDiagnosticQuestions(
        IReadOnlyList<QuestionPoolItem> pool,
        HashSet<Guid> recentQuestionIds,
        int totalQuestions,
        Random rng)
    {
        var selected = new List<Guid>();
        var topicGroups = pool
            .GroupBy(q => q.TopicId)
            .OrderBy(_ => rng.Next())
            .ToList();

        if (topicGroups.Count == 0) return selected;

        var perTopicBase = Math.Max(1, totalQuestions / topicGroups.Count);
        var remainder = totalQuestions % topicGroups.Count;

        foreach (var topicGroup in topicGroups)
        {
            var needed = perTopicBase + (remainder-- > 0 ? 1 : 0);
            var topicQuestions = topicGroup
                .OrderBy(q => recentQuestionIds.Contains(q.QuestionId) ? 1 : 0)
                .ThenBy(_ => rng.Next())
                .Take(needed)
                .Select(q => q.QuestionId)
                .ToList();

            selected.AddRange(topicQuestions);
        }

        return selected
            .Distinct()
            .Take(totalQuestions)
            .ToList();
    }

    private static int CalculateMockTimeLimitMinutes(int questionCount)
        => (int)Math.Ceiling(questionCount * 2.4);

    private enum WeaknessTier { VeryWeak, Weak, Average, Unknown }

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
