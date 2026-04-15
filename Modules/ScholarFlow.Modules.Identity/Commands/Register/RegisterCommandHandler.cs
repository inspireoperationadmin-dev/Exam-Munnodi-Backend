using MediatR;
using Microsoft.AspNetCore.Identity;
using ScholarFlow.Domain.Entities;
using ScholarFlow.Infrastructure.Services;
using ScholarFlow.Modules.Identity.DTOs;
using ScholarFlow.SharedKernel.Exceptions;
using ScholarFlow.SharedKernel.IntegrationEvents;

namespace ScholarFlow.Modules.Identity.Commands.Register;

public sealed class RegisterCommandHandler(
    UserManager<ApplicationUser> userManager,
    IPublisher                   publisher,
    ITokenService                tokenService)
    : IRequestHandler<RegisterCommand, AuthResponse>
{
    public async Task<AuthResponse> Handle(RegisterCommand request, CancellationToken ct)
    {
        // ── 1. Duplicate email check ──────────────────────────────────────────
        if (await userManager.FindByEmailAsync(request.Email) is not null)
            throw new ConflictException("An account with this email already exists.");

        // ── 2. Create Identity user ───────────────────────────────────────────
        var user = new ApplicationUser
        {
            Id       = Guid.NewGuid(),
            UserName = request.Email,
            Email    = request.Email
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

        // ── 3. Notify UserProfiles module to create the domain profile ────────
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

        // ── 4. Issue JWT ──────────────────────────────────────────────────────
        return new AuthResponse(
            AccessToken: tokenService.GenerateToken(user.Id, user.Email!, request.Role),
            ExpiresAt:   tokenService.TokenExpiresAt(),
            UserId:      user.Id,
            Email:       user.Email!,
            Role:        request.Role);
    }
}
