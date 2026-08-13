using Microsoft.EntityFrameworkCore;
using ScholarFlow.Domain.Entities;
using ScholarFlow.Domain.Enums;
using ScholarFlow.Domain.Interfaces.Repositories;

namespace ScholarFlow.Infrastructure.Persistence.Repositories;

public sealed class EfExamSessionRepository(ApplicationDbContext db) : IExamSessionRepository
{
    public Task<ExamSession?> GetByIdWithResponsesAsync(Guid sessionId, CancellationToken ct = default)
        => db.ExamSessions
            .Include(s => s.UserResponses)
                .ThenInclude(r => r.SelectedOption)
            .FirstOrDefaultAsync(s => s.Id == sessionId, ct);

    public Task<bool> HasCompletedExamSessionAsync(Guid userId, Guid paperId, CancellationToken ct = default)
        => db.ExamSessions.AnyAsync(
            s => s.UserId == userId
              && s.PaperId == paperId
              && s.Mode == ExamMode.PaperExam
              && s.Status == ExamSessionStatus.Completed,
            ct);

    public Task<UserResponse?> GetResponseAsync(Guid sessionId, Guid questionId, CancellationToken ct = default)
        => db.UserResponses
            .FirstOrDefaultAsync(r => r.SessionId == sessionId && r.QuestionId == questionId, ct);

    public Task<bool> QuestionBelongsToSessionAsync(Guid sessionId, Guid questionId, CancellationToken ct = default)
        => db.ExamSessionQuestions
            .AnyAsync(esq => esq.SessionId == sessionId && esq.QuestionId == questionId, ct);

    public async Task AddAsync(ExamSession session, CancellationToken ct = default)
        => await db.ExamSessions.AddAsync(session, ct);

    public async Task AddResponseAsync(UserResponse response, CancellationToken ct = default)
        => await db.UserResponses.AddAsync(response, ct);

    public async Task AddSessionQuestionAsync(ExamSessionQuestion question, CancellationToken ct = default)
        => await db.ExamSessionQuestions.AddAsync(question, ct);

    public Task SaveChangesAsync(CancellationToken ct = default)
        => db.SaveChangesAsync(ct);
}
