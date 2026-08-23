using System.Data;
using Microsoft.EntityFrameworkCore;
using ScholarFlow.Domain.Entities;
using ScholarFlow.Domain.Enums;
using ScholarFlow.Infrastructure.Persistence;
using ScholarFlow.SharedKernel.Exceptions;

namespace ScholarFlow.Modules.Examination.Services;

public sealed class ExamSessionStartPolicy(ApplicationDbContext db) : IExamSessionStartPolicy
{
    public const int MaximumPausedPracticeSessions = 3;
    private static readonly TimeSpan PracticeResumeWindow = TimeSpan.FromHours(24);

    public async Task EnsureCanStartAsync(
        ExamSession candidate,
        Guid? replaceSessionId,
        CancellationToken ct)
    {
        var openSessions = await db.ExamSessions
            .AsNoTracking()
            .Where(session => session.UserId == candidate.UserId
                           && session.Status == ExamSessionStatus.InProgress)
            .ToListAsync(ct);

        Validate(openSessions, candidate, replaceSessionId, DateTime.UtcNow);
    }

    public async Task PersistAsync(
        ExamSession candidate,
        IReadOnlyCollection<UserResponse> responses,
        IReadOnlyCollection<ExamSessionQuestion> sessionQuestions,
        Guid? replaceSessionId,
        CancellationToken ct)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(
            IsolationLevel.Serializable,
            ct);

        // Serialize session starts for this student only. This prevents duplicate mobile taps
        // and requests handled by different API instances from passing the limit together.
        await db.Users
            .FromSqlInterpolated($"SELECT * FROM AspNetUsers WITH (UPDLOCK, HOLDLOCK) WHERE Id = {candidate.UserId}")
            .AsNoTracking()
            .SingleAsync(ct);

        var now = DateTime.UtcNow;
        var openSessions = await db.ExamSessions
            .Where(session => session.UserId == candidate.UserId
                           && session.Status == ExamSessionStatus.InProgress)
            .OrderBy(session => session.Id)
            .ToListAsync(ct);

        var replacement = Validate(openSessions, candidate, replaceSessionId, now);
        replacement?.Abandon();

        foreach (var stalePractice in openSessions.Where(session => IsStalePractice(session, now)))
        {
            if (stalePractice.Status == ExamSessionStatus.InProgress)
            {
                stalePractice.Abandon();
            }
        }

        await db.ExamSessions.AddAsync(candidate, ct);
        await db.UserResponses.AddRangeAsync(responses, ct);
        await db.ExamSessionQuestions.AddRangeAsync(sessionQuestions, ct);
        await db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
    }

    private static ExamSession? Validate(
        IReadOnlyCollection<ExamSession> openSessions,
        ExamSession candidate,
        Guid? replaceSessionId,
        DateTime now)
    {
        var resumableSessions = openSessions
            .Where(session => IsResumable(session, now))
            .ToList();

        ExamSession? replacement = null;
        if (replaceSessionId.HasValue)
        {
            if (!candidate.Mode.IsPracticeMode())
            {
                throw new BadRequestException("Only a practice session can be replaced.");
            }

            replacement = resumableSessions.SingleOrDefault(session => session.Id == replaceSessionId.Value)
                ?? throw new BadRequestException("The practice session selected for replacement is no longer resumable.");

            if (!replacement.Mode.IsPracticeMode() || !HasSameSource(replacement, candidate))
            {
                throw new BadRequestException("The replacement session must match the same practice.");
            }

            resumableSessions.Remove(replacement);
        }

        if (candidate.Mode.IsTimedMode())
        {
            var activeTimed = resumableSessions
                .Where(session => session.Mode.IsTimedMode())
                .OrderByDescending(session => session.LastActivityAt)
                .FirstOrDefault();

            if (activeTimed is not null)
            {
                throw Conflict(
                    "ACTIVE_TIMED_SESSION_EXISTS",
                    "Finish or leave your current timed exam before starting another one.",
                    activeTimed.Id,
                    ["resume", "cancel"]);
            }

            return replacement;
        }

        var duplicatePractice = resumableSessions
            .Where(session => session.Mode.IsPracticeMode() && HasSameSource(session, candidate))
            .OrderByDescending(session => session.LastActivityAt)
            .FirstOrDefault();

        if (duplicatePractice is not null)
        {
            throw Conflict(
                "PRACTICE_SESSION_EXISTS",
                "This practice already has a paused session.",
                duplicatePractice.Id,
                ["resume", "startFresh", "cancel"]);
        }

        var practices = resumableSessions
            .Where(session => session.Mode.IsPracticeMode())
            .OrderByDescending(session => session.LastActivityAt)
            .ToList();

        if (practices.Count >= MaximumPausedPracticeSessions)
        {
            throw new SessionStartConflictException(
                "PRACTICE_LIMIT_REACHED",
                $"You can pause up to {MaximumPausedPracticeSessions} practice sessions. Resume or replace one before starting another.",
                practices.Select(session => session.Id).ToList(),
                ["manageSessions", "cancel"]);
        }

        return replacement;
    }

    private static bool IsResumable(ExamSession session, DateTime now)
        => session.Mode.IsPracticeMode()
            ? session.LastActivityAt >= now.Subtract(PracticeResumeWindow)
            : !session.ExpiresAt.HasValue || session.ExpiresAt.Value > now;

    private static bool IsStalePractice(ExamSession session, DateTime now)
        => session.Mode.IsPracticeMode()
        && session.LastActivityAt < now.Subtract(PracticeResumeWindow);

    private static bool HasSameSource(ExamSession existing, ExamSession candidate)
    {
        if (existing.Mode != candidate.Mode)
        {
            return false;
        }

        return candidate.Mode switch
        {
            ExamMode.PaperPractice => existing.PaperId == candidate.PaperId,
            ExamMode.TopicPractice => existing.TopicId == candidate.TopicId,
            _ => false
        };
    }

    private static SessionStartConflictException Conflict(
        string code,
        string message,
        Guid sessionId,
        IReadOnlyList<string> allowedActions)
        => new(code, message, [sessionId], allowedActions);
}
