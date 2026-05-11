using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using ScholarFlow.Domain.Entities;
using ScholarFlow.Infrastructure.Persistence;
using ScholarFlow.Infrastructure.Services;
using ScholarFlow.Modules.Identity.DTOs;
using ScholarFlow.SharedKernel.Exceptions;
using ScholarFlow.SharedKernel.IntegrationEvents;

namespace ScholarFlow.Modules.Identity.Commands.Register;

public sealed class RegisterCommandHandler(
    UserManager<ApplicationUser> userManager,
    IPublisher                   publisher,
    ITokenService                tokenService,
    ApplicationDbContext         db)
    : IRequestHandler<RegisterCommand, AuthResponse>
{
    public async Task<AuthResponse> Handle(RegisterCommand request, CancellationToken ct)
    {
        var email = request.Email.Trim().ToLowerInvariant();

        // ── 1. Ensure email was OTP-verified ──────────────────────────────────
        var verifiedOtp = await db.OtpCodes
            .Where(o => o.Email == email && o.IsVerified && o.ExpiresAt > DateTime.UtcNow)
            .FirstOrDefaultAsync(ct);

        if (verifiedOtp is null)
            throw new BadRequestException("Please verify your email address before registering.");

        // ── 2. Duplicate email check ──────────────────────────────────────────
        if (await userManager.FindByEmailAsync(email) is not null)
            throw new ConflictException("An account with this email already exists.");

        // ── 3. Create Identity user ───────────────────────────────────────────
        var user = new ApplicationUser
        {
            Id       = Guid.NewGuid(),
            UserName = email,
            Email    = email
        };

        var createResult = await userManager.CreateAsync(user, request.Password);
        if (!createResult.Succeeded)
            throw new BadRequestException(
                "Failed to create account.",
                [..createResult.Errors.Select(e => e.Description)]);

        var roleResult = await userManager.AddToRoleAsync(user, request.Role);
        if (!roleResult.Succeeded)
        {
            await userManager.DeleteAsync(user);
            throw new BadRequestException(
                "Failed to assign role.",
                [..roleResult.Errors.Select(e => e.Description)]);
        }

        // ── 4. Consume the verified OTP (delete it so it can't be reused) ─────
        db.OtpCodes.Remove(verifiedOtp);

        // ── 5. Notify UserProfiles module to create the domain profile ────────
        // TeacherCode generation is intentionally omitted here —
        // that is a UserProfiles module concern handled in the event handler.
        await publisher.Publish(new UserRegisteredIntegrationEvent(
            EventId:       Guid.NewGuid(),
            OccurredOn:    DateTime.UtcNow,
            UserId:        user.Id,
            Email:         user.Email!,
            Role:          request.Role,
            FullName:      request.FullName,
            PhoneNumber:   request.PhoneNumber,
            SubjectId:     request.SubjectId,
            Qualification: request.Qualification,
            Bio:           request.Bio), ct);

        await db.SaveChangesAsync(ct);

        // ── 6. Issue JWT ──────────────────────────────────────────────────────
        return new AuthResponse(
            AccessToken: tokenService.GenerateToken(user.Id, user.Email!, request.Role),
            ExpiresAt:   tokenService.TokenExpiresAt(),
            UserId:      user.Id,
            Email:       user.Email!,
            Role:        request.Role);
    }
}
