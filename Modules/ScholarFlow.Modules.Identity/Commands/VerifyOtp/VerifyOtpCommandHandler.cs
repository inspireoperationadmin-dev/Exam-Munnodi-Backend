using System.Security.Cryptography;
using System.Text;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using ScholarFlow.Domain.Entities;
using ScholarFlow.Infrastructure.Persistence;
using ScholarFlow.Modules.Identity.DTOs;
using ScholarFlow.Infrastructure.Services;
using ScholarFlow.SharedKernel.Exceptions;

namespace ScholarFlow.Modules.Identity.Commands.VerifyOtp;

public sealed class VerifyOtpCommandHandler(
    ApplicationDbContext         db,
    UserManager<ApplicationUser> userManager,
    ITokenService                tokenService)
    : IRequestHandler<VerifyOtpCommand, AuthResponse>
{
    public async Task<AuthResponse> Handle(VerifyOtpCommand request, CancellationToken ct)
    {
        var email    = request.Email.Trim().ToLowerInvariant();
        var codeHash = HashCode(request.Code.Trim());

        // ── 1. Find a valid, unused, unexpired OTP ────────────────────────────
        var otp = await db.OtpCodes
            .Where(o => o.Email == email && !o.IsVerified && o.ExpiresAt > DateTime.UtcNow)
            .OrderByDescending(o => o.CreatedAt)
            .FirstOrDefaultAsync(ct);

        if (otp is null || otp.CodeHash != codeHash)
            throw new BadRequestException("Invalid or expired OTP code. Please request a new one.");

        // ── 2. Mark OTP verified (extends expiry 15 min — see OtpCode.MarkVerified) ─
        otp.MarkVerified();

        // ── 3. Confirm email on the Identity user ─────────────────────────────
        var user = await userManager.FindByEmailAsync(email)
            ?? throw new NotFoundException("User not found.");

        user.EmailConfirmed = true;
        var updateResult = await userManager.UpdateAsync(user);
        if (!updateResult.Succeeded)
            throw new BadRequestException("Failed to confirm email.");

        // ── 4. Clean up: delete this OTP now — it served its purpose ──────────
        db.OtpCodes.Remove(otp);
        await db.SaveChangesAsync(ct);

        // ── 5. Issue new JWT and return full AuthResponse ───────────────────
        var roles = await userManager.GetRolesAsync(user);
        var role  = roles.FirstOrDefault() ?? string.Empty;

        return new AuthResponse(
            AccessToken:     tokenService.GenerateToken(user.Id, user.Email!, role),
            ExpiresAt:       tokenService.TokenExpiresAt(),
            UserId:          user.Id,
            Email:           user.Email!,
            Role:            role,
            IsEmailVerified: true,
            IsProfileSetup:  user.IsProfileSetup);
    }

    private static string HashCode(string code)
        => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(code)));
}