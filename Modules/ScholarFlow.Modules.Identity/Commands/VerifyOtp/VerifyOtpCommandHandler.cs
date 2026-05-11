using System.Security.Cryptography;
using System.Text;
using MediatR;
using Microsoft.EntityFrameworkCore;
using ScholarFlow.Infrastructure.Persistence;
using ScholarFlow.SharedKernel.Exceptions;

namespace ScholarFlow.Modules.Identity.Commands.VerifyOtp;

public sealed class VerifyOtpCommandHandler(ApplicationDbContext db)
    : IRequestHandler<VerifyOtpCommand>
{
    public async Task Handle(VerifyOtpCommand request, CancellationToken ct)
    {
        var email    = request.Email.Trim().ToLowerInvariant();
        var codeHash = HashCode(request.Code.Trim());

        var otp = await db.OtpCodes
            .Where(o => o.Email == email && !o.IsVerified && o.ExpiresAt > DateTime.UtcNow)
            .OrderByDescending(o => o.CreatedAt)
            .FirstOrDefaultAsync(ct);

        if (otp is null || otp.CodeHash != codeHash)
            throw new BadRequestException("Invalid or expired OTP code. Please request a new one.");

        otp.MarkVerified();
        await db.SaveChangesAsync(ct);
    }

    private static string HashCode(string code)
        => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(code)));
}
