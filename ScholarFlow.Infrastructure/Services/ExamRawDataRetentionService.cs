using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ScholarFlow.Domain.Enums;
using ScholarFlow.Infrastructure.Persistence;

namespace ScholarFlow.Infrastructure.Services;

public sealed class ExamRawDataRetentionService(
    IServiceScopeFactory scopeFactory,
    ILogger<ExamRawDataRetentionService> logger)
    : BackgroundService
{
    private static readonly TimeSpan StartupDelay = TimeSpan.FromMinutes(5);
    private static readonly TimeSpan ScanInterval = TimeSpan.FromHours(6);
    private static readonly TimeSpan RetentionPeriod = TimeSpan.FromDays(3);
    private const int BatchSize = 500;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.Delay(StartupDelay, stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            await CleanupAsync(stoppingToken);
            await Task.Delay(ScanInterval, stoppingToken);
        }
    }

    private async Task CleanupAsync(CancellationToken ct)
    {
        try
        {
            await using var scope = scopeFactory.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var cutoff = DateTime.UtcNow.Subtract(RetentionPeriod);

            var totalSessions = 0;
            var totalResponses = 0;
            var totalSessionQuestions = 0;

            while (!ct.IsCancellationRequested)
            {
                var sessionIds = await db.ExamSessions
                    .Where(session => session.EndTime != null
                        && session.EndTime < cutoff
                        && session.Status != ExamSessionStatus.InProgress)
                    .Where(session =>
                        db.UserResponses.Any(response => response.SessionId == session.Id)
                        || db.ExamSessionQuestions.Any(question => question.SessionId == session.Id))
                    .OrderBy(session => session.EndTime)
                    .Select(session => session.Id)
                    .Take(BatchSize)
                    .ToListAsync(ct);

                if (sessionIds.Count == 0)
                {
                    break;
                }

                totalResponses += await db.UserResponses
                    .Where(response => sessionIds.Contains(response.SessionId))
                    .ExecuteDeleteAsync(ct);

                totalSessionQuestions += await db.ExamSessionQuestions
                    .Where(question => sessionIds.Contains(question.SessionId))
                    .ExecuteDeleteAsync(ct);

                totalSessions += sessionIds.Count;
            }

            if (totalSessions == 0)
            {
                return;
            }

            logger.LogInformation(
                "[ExamRawDataRetention] Deleted {ResponseCount} response row(s) and {QuestionCount} session-question row(s) for {SessionCount} closed session(s) older than {RetentionDays} days.",
                totalResponses,
                totalSessionQuestions,
                totalSessions,
                RetentionPeriod.TotalDays);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            // Normal shutdown.
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "[ExamRawDataRetention] Cleanup failed; will retry next cycle.");
        }
    }
}
