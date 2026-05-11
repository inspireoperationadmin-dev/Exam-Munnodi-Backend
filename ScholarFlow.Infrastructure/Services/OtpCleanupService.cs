using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ScholarFlow.Infrastructure.Persistence;

namespace ScholarFlow.Infrastructure.Services;

/// <summary>
/// Runs every hour and deletes OtpCode rows that are outside the 1-hour rate-limit
/// window (i.e. created more than 1 hour ago). This keeps the table clean without
/// affecting the per-email rate limiting count used by SendOtpCommandHandler.
/// </summary>
public sealed class OtpCleanupService(
    IServiceScopeFactory scopeFactory,
    ILogger<OtpCleanupService> logger)
    : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromHours(1);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Wait a short delay at startup so the DB is ready before first cleanup.
        await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            await CleanupAsync(stoppingToken);
            await Task.Delay(Interval, stoppingToken);
        }
    }

    private async Task CleanupAsync(CancellationToken ct)
    {
        try
        {
            await using var scope = scopeFactory.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            var cutoff  = DateTime.UtcNow.AddHours(-1);
            var deleted = await db.OtpCodes
                .Where(o => o.CreatedAt < cutoff)
                .ExecuteDeleteAsync(ct);

            if (deleted > 0)
                logger.LogInformation("[OtpCleanup] Deleted {Count} expired OTP record(s).", deleted);
        }
        catch (Exception ex) when (!ct.IsCancellationRequested)
        {
            logger.LogError(ex, "[OtpCleanup] Cleanup failed — will retry next cycle.");
        }
    }
}
