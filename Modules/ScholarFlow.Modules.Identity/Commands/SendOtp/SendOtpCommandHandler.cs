using System.Security.Cryptography;
using System.Text;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ScholarFlow.Domain.Entities;
using ScholarFlow.Infrastructure.Persistence;
using ScholarFlow.Infrastructure.Services;
using ScholarFlow.SharedKernel.Exceptions;

namespace ScholarFlow.Modules.Identity.Commands.SendOtp;

public sealed class SendOtpCommandHandler(
    ApplicationDbContext db,
    IEmailService        emailService,
    UserManager<ApplicationUser> userManager,
    ILogger<SendOtpCommandHandler> logger)
    : IRequestHandler<SendOtpCommand>
{
    private const int MaxSendsPerWindow = 3;
    private static readonly TimeSpan SendWindow = TimeSpan.FromHours(6);
    private static readonly TimeSpan ResendCooldown = TimeSpan.FromMinutes(1);

    public async Task Handle(SendOtpCommand request, CancellationToken ct)
    {
        var email      = request.Email.Trim().ToLowerInvariant();
        var now        = DateTime.UtcNow;
        var windowStart = now.Subtract(SendWindow);
        var code = RandomNumberGenerator.GetInt32(100_000, 1_000_000).ToString();
        var codeHash = HashCode(code);

        var existingUser = await userManager.FindByEmailAsync(email);
        if (existingUser is { EmailConfirmed: true })
            return;

        OtpCode otp;
        await using (var transaction = await db.Database.BeginTransactionAsync(
                         System.Data.IsolationLevel.Serializable, ct))
        {
            var latest = await db.OtpCodes
                .Where(o => o.Email == email)
                .OrderByDescending(o => o.CreatedAt)
                .FirstOrDefaultAsync(ct);

            if (latest is not null && now - latest.CreatedAt < ResendCooldown)
                throw new TooManyRequestsException(
                    "Please wait before requesting another verification code.");

            var recentCount = await db.OtpCodes
                .CountAsync(o => o.Email == email && o.CreatedAt >= windowStart, ct);

            if (recentCount >= MaxSendsPerWindow)
                throw new TooManyRequestsException(
                    "Too many verification codes requested. Please try again later.");

            var old = await db.OtpCodes
                .Where(o => o.Email == email && o.CreatedAt < windowStart)
                .ToListAsync(ct);
            db.OtpCodes.RemoveRange(old);

            var activeCodes = await db.OtpCodes
                .Where(o => o.Email == email && o.ExpiresAt > now)
                .ToListAsync(ct);
            foreach (var activeCode in activeCodes)
                activeCode.Invalidate();

            otp = OtpCode.Create(email, codeHash);
            db.OtpCodes.Add(otp);
            await db.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);
        }

        try
        {
            await emailService.SendOtpAsync(email, code, ct);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex,
                "[OTP] Email delivery failed for {Email}.",
                email);

            db.OtpCodes.Remove(otp);
            await db.SaveChangesAsync(ct);
            throw;
        }
    }

    private static string HashCode(string code)
        => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(code)));
}
