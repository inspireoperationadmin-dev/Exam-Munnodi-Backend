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
    ITokenService                tokenService,
    IRegistrationTicketService   registrationTicketService)
    : IRequestHandler<VerifyOtpCommand, VerifyOtpResponse>
{
    private const int MaximumFailedAttempts = 3;

    public async Task<VerifyOtpResponse> Handle(VerifyOtpCommand request, CancellationToken ct)
    {
        var email    = request.Email.Trim().ToLowerInvariant();
        var codeHash = HashCode(request.Code.Trim());

        // ── 1. Find a valid, unused, unexpired OTP ────────────────────────────
        var otp = await db.OtpCodes
            .Where(o => o.Email == email && !o.IsVerified && o.ExpiresAt > DateTime.UtcNow)
            .OrderByDescending(o => o.CreatedAt)
            .FirstOrDefaultAsync(ct);

        if (otp is null)
            throw new BadRequestException("Invalid or expired verification code.");

        if (!HashesMatch(otp.CodeHash, codeHash))
        {
            otp.RecordFailedAttempt(MaximumFailedAttempts);
            await db.SaveChangesAsync(ct);
            throw new BadRequestException("Invalid or expired verification code.");
        }

        otp.MarkVerified();

        var user = await userManager.FindByEmailAsync(email);
        if (user is null)
        {
            await db.SaveChangesAsync(ct);
            var ticket = registrationTicketService.Create(otp.Id, email, otp.ExpiresAt);

            return new VerifyOtpResponse(
                Email: email,
                RequiresAccountCreation: true,
                RegistrationTicket: ticket.Token,
                RegistrationTicketExpiresAt: ticket.ExpiresAt,
                Authentication: null);
        }

        // Compatibility for accounts created by the previous registration flow.
        user.EmailConfirmed = true;
        var updateResult = await userManager.UpdateAsync(user);
        if (!updateResult.Succeeded)
            throw new BadRequestException("Failed to confirm email.");

        var otpRows = await db.OtpCodes.Where(o => o.Email == email).ToListAsync(ct);
        db.OtpCodes.RemoveRange(otpRows);
        await db.SaveChangesAsync(ct);

        var roles = await userManager.GetRolesAsync(user);
        var role  = roles.FirstOrDefault() ?? string.Empty;

        var authentication = new AuthResponse(
            AccessToken:     tokenService.GenerateToken(user.Id, user.Email!, role),
            ExpiresAt:       tokenService.TokenExpiresAt(),
            UserId:          user.Id,
            Email:           user.Email!,
            Role:            role,
            IsEmailVerified: true,
            IsProfileSetup:  user.IsProfileSetup);

        return new VerifyOtpResponse(
            Email: email,
            RequiresAccountCreation: false,
            RegistrationTicket: null,
            RegistrationTicketExpiresAt: null,
            Authentication: authentication);
    }

    private static string HashCode(string code)
        => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(code)));

    private static bool HashesMatch(string storedHash, string submittedHash)
        => CryptographicOperations.FixedTimeEquals(
            Convert.FromHexString(storedHash),
            Convert.FromHexString(submittedHash));
}
