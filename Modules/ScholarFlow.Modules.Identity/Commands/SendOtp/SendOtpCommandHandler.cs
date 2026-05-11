using System.Security.Cryptography;
using System.Text;
using MediatR;
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
    ILogger<SendOtpCommandHandler> logger)
    : IRequestHandler<SendOtpCommand>
{
    private const int MaxSendsPerHour = 3;

    public async Task Handle(SendOtpCommand request, CancellationToken ct)
    {
        var email      = request.Email.Trim().ToLowerInvariant();
        var now        = DateTime.UtcNow;
        var oneHourAgo = now.AddHours(-1);

        // ── 1. Rate limit: max 3 OTP sends per email per hour ─────────────────
        var recentCount = await db.OtpCodes
            .CountAsync(o => o.Email == email && o.CreatedAt >= oneHourAgo, ct);

        if (recentCount >= MaxSendsPerHour)
            throw new TooManyRequestsException(
                "Too many OTP requests. Please wait before requesting a new code.");

        // ── 2. Generate a cryptographically random 6-digit code ───────────────
        var code     = RandomNumberGenerator.GetInt32(100_000, 999_999).ToString();
        var codeHash = HashCode(code);

        // ── 3. Remove OTPs older than 1 hour for this email ───────────────────
        //    (recent ones are kept so the rate-limit count above stays accurate)
        var old = await db.OtpCodes
            .Where(o => o.Email == email && o.CreatedAt < oneHourAgo)
            .ToListAsync(ct);

        db.OtpCodes.RemoveRange(old);

        // ── 4. Persist the new OTP ────────────────────────────────────────────
        var otp = OtpCode.Create(email, codeHash);
        db.OtpCodes.Add(otp);
        await db.SaveChangesAsync(ct);

        // ── 5. Attempt email delivery (non-fatal in dev) ──────────────────────
        try
        {
            await emailService.SendOtpAsync(email, code, ct);
        }
        catch (Exception ex)
        {
            // Email delivery failed but the OTP is already saved in the DB.
            // The code is also printed in the server log above.
            logger.LogWarning(ex,
                "[OTP] Email delivery failed for {Email}. " +
                "OTP was saved to DB — check server logs for the code.",
                email);
        }
    }

    private static string HashCode(string code)
        => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(code)));
}
