using MediatR;
using Microsoft.AspNetCore.Identity;
using ScholarFlow.Domain.Entities;
using ScholarFlow.Infrastructure.Services;
using ScholarFlow.Modules.Identity.Commands.SendOtp;
using ScholarFlow.Modules.Identity.DTOs;
using ScholarFlow.SharedKernel.Exceptions;
using ScholarFlow.SharedKernel.IntegrationEvents;

namespace ScholarFlow.Modules.Identity.Commands.Register;

public sealed class RegisterCommandHandler(
    UserManager<ApplicationUser> userManager,
    IMediator                   mediator,
    ITokenService                tokenService)
    : IRequestHandler<RegisterCommand, AuthResponse>
{
    public async Task<AuthResponse> Handle(RegisterCommand request, CancellationToken ct)
    {
        var email = request.Email.Trim().ToLowerInvariant();

        // ── 1. Duplicate email check ──────────────────────────────────────────
        if (await userManager.FindByEmailAsync(email) is not null)
            throw new ConflictException("An account with this email already exists.");

        // ── 2. Create Identity user with EmailConfirmed = false ───────────────
        var user = new ApplicationUser
        {
            Id             = Guid.NewGuid(),
            UserName       = email,
            Email          = email,
            EmailConfirmed = false   // enforced — cannot log in or access app until verified
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

        // ── 3. Notify UserProfiles module to create domain profile shell ──────
        await mediator.Publish(new UserRegisteredIntegrationEvent(
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

        // ── 4. Trigger OTP send (same handler reused — rate limit applies) ────
        await mediator.Send(new SendOtpCommand(email), ct);

        // ── 5. Issue JWT — IsEmailVerified=false tells frontend to go to OTP page
        //       Token is valid but all protected routes enforce EmailConfirmed below.
        return new AuthResponse(
            AccessToken:     tokenService.GenerateToken(user.Id, user.Email!, request.Role),
            ExpiresAt:       tokenService.TokenExpiresAt(),
            UserId:          user.Id,
            Email:           user.Email!,
            Role:            request.Role,
            IsEmailVerified: false,
            IsProfileSetup:  false);
    }
}