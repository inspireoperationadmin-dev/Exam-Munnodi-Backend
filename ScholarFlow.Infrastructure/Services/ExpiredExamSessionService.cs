using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ScholarFlow.Domain.Enums;
using ScholarFlow.Infrastructure.Persistence;

namespace ScholarFlow.Infrastructure.Services;

public sealed class ExpiredExamSessionService(
    IServiceScopeFactory scopeFactory,
    ILogger<ExpiredExamSessionService> logger)
    : BackgroundService
{
    private static readonly TimeSpan ScanInterval = TimeSpan.FromMinutes(1);
    private const int BatchSize = 100;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await CloseExpiredSessionsAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to close expired exam sessions.");
            }

            await Task.Delay(ScanInterval, stoppingToken);
        }
    }

    private async Task CloseExpiredSessionsAsync(CancellationToken ct)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var now = DateTime.UtcNow;

        var sessions = await db.ExamSessions
            .Include(s => s.UserResponses)
            .Where(s => s.Status == ExamSessionStatus.InProgress
                && s.ExpiresAt != null
                && s.ExpiresAt <= now)
            .OrderBy(s => s.ExpiresAt)
            .Take(BatchSize)
            .ToListAsync(ct);

        if (sessions.Count == 0)
        {
            return;
        }

        var questionIds = sessions
            .SelectMany(s => s.UserResponses.Select(r => r.QuestionId))
            .Distinct()
            .ToList();

        var gradingRows = await db.Questions
            .Where(q => questionIds.Contains(q.Id))
            .Select(q => new
            {
                QuestionId = q.Id,
                q.Marks,
                CorrectOptionId = q.Options
                    .Where(o => o.IsCorrect)
                    .Select(o => (Guid?)o.Id)
                    .FirstOrDefault()
            })
            .ToListAsync(ct);

        var marksPerQuestion = gradingRows.ToDictionary(row => row.QuestionId, row => row.Marks);
        var correctOptionPerQuestion = gradingRows.ToDictionary(
            row => row.QuestionId,
            row => row.CorrectOptionId ?? Guid.Empty);

        foreach (var session in sessions)
        {
            session.Complete(marksPerQuestion, correctOptionPerQuestion, ExamSessionStatus.TimedOut);
        }

        await db.SaveChangesAsync(ct);
    }
}
