using Dapper;
using MediatR;
using ScholarFlow.Domain.Entities;
using ScholarFlow.Domain.Enums;
using ScholarFlow.Domain.Interfaces;
using ScholarFlow.Domain.Interfaces.Repositories;
using ScholarFlow.Modules.Academic.Public;
using ScholarFlow.Modules.Examination.DTOs;
using ScholarFlow.Modules.UserProfiles.Public;
using ScholarFlow.SharedKernel.Exceptions;

namespace ScholarFlow.Modules.Examination.Commands.StartExamSession;

public sealed class StartExamSessionCommandHandler(
    IExamSessionRepository examRepo,
    IAcademicApi           academicApi,
    IUserProfilesApi       userProfilesApi,
    IExplanationRepository explanationRepo,
    ISqlConnectionFactory  sql,             // <-- Injected for high-performance Dapper query
    ICurrentUser           currentUser)
    : IRequestHandler<StartExamSessionCommand, StartSessionResultDto>
{
    public async Task<StartSessionResultDto> Handle(StartExamSessionCommand request, CancellationToken ct)
    {
        // 1. Load paper summary via Academic module public API
        var paper = await academicApi.GetPaperSummaryAsync(request.PaperId, ct)
            ?? throw new NotFoundException("Paper not found.");

        // 2. Check paper access — public OR connected teacher's paper
        if (!paper.IsPublic)
        {
            if (paper.CreatedByTeacherId is null)
                throw new ForbiddenException("You do not have access to this paper.");

            bool connected = await userProfilesApi.IsStudentConnectedToTeacherAsync(
                currentUser.UserId, paper.CreatedByTeacherId.Value, ct);

            if (!connected)
                throw new ForbiddenException("You do not have access to this paper.");
        }

        // 4. Load all questions summary via Academic module public API (stores CorrectOptionId and Marks)
        var questionsSummary = await academicApi.GetQuestionsForExamAsync(request.PaperId, ct);

        if (questionsSummary.Count == 0)
            throw new BadRequestException("This paper has no questions.");

        // 5. Create session
        var session = ExamSession.Start(
            userId:       currentUser.UserId,
            paperId:      request.PaperId,
            subjectId:    paper.SubjectId,
            mode:         request.Mode,
            timeLimitMinutes: request.Mode == ExamMode.Practice ? null : paper.TimeLimit);

        await examRepo.AddAsync(session, ct);

        // 6. Create UserResponse + ExamSessionQuestion for each question
        for (int i = 0; i < questionsSummary.Count; i++)
        {
            var q = questionsSummary[i];

            await examRepo.AddResponseAsync(new UserResponse
            {
                Id             = Guid.NewGuid(),
                SessionId      = session.Id,
                QuestionId     = q.QuestionId,
                OrderIndex     = i + 1, // Store layout index natively inside response
                ResponseStatus = ResponseStatus.Unvisited
            }, ct);

            await examRepo.AddSessionQuestionAsync(new ExamSessionQuestion
            {
                Id         = Guid.NewGuid(),
                SessionId  = session.Id,
                QuestionId = q.QuestionId,
                OrderIndex = i + 1
            }, ct);
        }

        await examRepo.SaveChangesAsync(ct);

        // 7. Fetch the detailed question texts and option details using Dapper
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
            WHERE q.PaperId   = @PaperId
              AND q.IsDeleted  = 0
            ORDER BY q.OrderIndex, o.Label
            """,
            new { PaperId = request.PaperId });

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

        // 8. If Practice Mode, retrieve all explanation sections in a single bulk query
        var explanationsMap = new Dictionary<Guid, Explanation>();
        var summaryLookup = questionsSummary.ToDictionary(q => q.QuestionId);

        if (request.Mode == ExamMode.Practice)
        {
            var questionIds = questionsSummary.Select(q => q.QuestionId).ToList();
            var explanationsList = await explanationRepo.GetByQuestionIdsAsync(questionIds, ct);
            explanationsMap = explanationsList.ToDictionary(e => e.QuestionId);
        }

        // 9. Map final DTO array with security conditional checks
        var examQuestions = questionsDict.Select(kv =>
        {
            var questionId = kv.Key;
            var details    = kv.Value;

            Guid? correctOptionId = null;
            string? explanationText = null;

            if (request.Mode == ExamMode.Practice)
            {
                // Pull correct option safely from public API summary lookup
                if (summaryLookup.TryGetValue(questionId, out var summary))
                {
                    correctOptionId = summary.CorrectOptionId;
                }

                // Returns a clean, non-LaTeX string with the title on the first line [1]
                if (explanationsMap.TryGetValue(questionId, out var explanation) && explanation.Sections.Any())
                {
                    explanationText = string.Join("\n\n", explanation.Sections
                        .OrderBy(s => s.OrderIndex)
                        .Select(s => $"{s.Title}\n{s.Content}"));
                }
            }

            return new ExamQuestionDto(
                Id:               questionId,
                OrderIndex:       details.Order,
                QuestionText:     details.Text,
                QuestionImageUrl: details.Image,
                Marks:            details.Marks,
                Options:          details.Options,
                CorrectOptionId:  correctOptionId,
                ExplanationText:  explanationText
            );
        })
        .OrderBy(q => q.OrderIndex)
        .ToList();

        // 10. Return complete atomic package
        return new StartSessionResultDto(
            SessionId:     session.Id,
            StartTime:     session.StartTime,
            ServerNow:     DateTime.UtcNow,
            ExpiresAt:     session.ExpiresAt,
            TimeLimitMinutes: session.TimeLimitMinutes,
            Mode:          session.Mode.ToString(),
            QuestionCount: questionsSummary.Count,
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
    }
}
