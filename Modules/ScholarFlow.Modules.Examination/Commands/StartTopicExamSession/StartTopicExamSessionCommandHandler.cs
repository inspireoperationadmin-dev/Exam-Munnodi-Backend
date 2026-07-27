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
    public async Task<StartSessionResultDto> Handle(StartTopicExamSessionCommand request, CancellationToken ct)
    {
        using var conn = sql.CreateConnection();

        // 1. Fetch SubjectId and all available QuestionIds belonging to this Topic
        var subjectId = await conn.QuerySingleOrDefaultAsync<Guid?>("""
            SELECT SubjectId FROM Topics WHERE Id = @TopicId AND IsDeleted = 0
            """, new { request.TopicId });

        if (!subjectId.HasValue)
            throw new NotFoundException("Topic not found.");

        var questionIds = (await conn.QueryAsync<Guid>("""
            SELECT q.Id 
            FROM Questions q
            INNER JOIN SubTopics st ON st.Id = q.SubTopicId
            WHERE st.TopicId = @TopicId AND q.IsDeleted = 0
            """, new { request.TopicId })).ToList();

        if (questionIds.Count == 0)
            throw new BadRequestException("No questions available for this topic.");

        // 2. Shuffle and take the requested limit (e.g., 10 or 20) in memory
        var rng = new Random();
        var selectedIds = questionIds
            .OrderBy(_ => rng.Next())
            .Take(request.Limit)
            .ToList();

        // 3. Create dynamic topic exam session (PaperId = null)
        var session = ExamSession.Start(
            userId:         currentUser.UserId,
            paperId:        null, 
            subjectId:      subjectId.Value,
            mode:           request.Mode,
            timeLimitMinutes: request.Mode == ExamMode.Practice ? null : selectedIds.Count);

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

        // 6. If Practice Mode, retrieve all explanation sections in a single bulk query
        var explanationsMap = new Dictionary<Guid, Explanation>();
        if (request.Mode == ExamMode.Practice)
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

            if (request.Mode == ExamMode.Practice)
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
}
