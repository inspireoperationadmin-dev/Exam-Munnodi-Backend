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
    IAcademicApi academicApi,
    IUserProfilesApi userProfilesApi,
    ICurrentUser currentUser)
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

        // 3. Exam mode: one attempt only
        if (!request.IsPractice)
        {
            bool alreadyAttempted = await examRepo.HasCompletedExamSessionAsync(
                currentUser.UserId, request.PaperId, ct);

            if (alreadyAttempted)
                throw new ConflictException("You have already completed this paper in exam mode.");
        }

        // 4. Load all questions via Academic module public API
        var questions = await academicApi.GetQuestionsForExamAsync(request.PaperId, ct);

        if (questions.Count == 0)
            throw new BadRequestException("This paper has no questions.");

        // 5. Create session
        var session = ExamSession.Start(
            userId:       currentUser.UserId,
            paperId:      request.PaperId,
            subjectId:    null,
            isPractice:   request.IsPractice);

        await examRepo.AddAsync(session, ct);

        // 6. Create UserResponse + ExamSessionQuestion for each question
        for (int i = 0; i < questions.Count; i++)
        {
            var q = questions[i];

            await examRepo.AddResponseAsync(new UserResponse
            {
                Id             = Guid.NewGuid(),
                SessionId      = session.Id,
                QuestionId     = q.QuestionId,
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

        return new StartSessionResultDto(
            SessionId:     session.Id,
            StartTime:     session.StartTime,
            IsPractice:    session.IsPractice,
            QuestionCount: questions.Count);
    }
}
