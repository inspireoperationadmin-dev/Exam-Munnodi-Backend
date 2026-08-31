using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using ScholarFlow.Domain.Entities;
using ScholarFlow.Infrastructure.Services;
using ScholarFlow.Infrastructure.Persistence;
using ScholarFlow.Modules.Identity.DTOs;
using ScholarFlow.SharedKernel.Exceptions;
using ScholarFlow.SharedKernel.IntegrationEvents;

namespace ScholarFlow.Modules.Identity.Commands.Register;

public sealed class RegisterCommandHandler(
    UserManager<ApplicationUser> userManager,
    IMediator                   mediator,
    ITokenService                tokenService,
    IRegistrationTicketService   registrationTicketService,
    ApplicationDbContext         db)
    : IRequestHandler<RegisterCommand, AuthResponse>
{
    public async Task<AuthResponse> Handle(RegisterCommand request, CancellationToken ct)
    {
        var email = request.Email.Trim().ToLowerInvariant();

        if (!registrationTicketService.TryValidate(
                request.RegistrationTicket, out var ticket) ||
            !string.Equals(ticket.Email, email, StringComparison.OrdinalIgnoreCase))
        {
            throw new BadRequestException("The registration verification has expired. Please verify your email again.");
        }

        var verifiedOtp = await db.OtpCodes
            .SingleOrDefaultAsync(o =>
                o.Id == ticket.OtpId &&
                o.Email == email &&
                o.IsVerified &&
                o.ExpiresAt > DateTime.UtcNow, ct);

        if (verifiedOtp is null)
            throw new BadRequestException("The registration verification has expired or has already been used.");

        if (await userManager.FindByEmailAsync(email) is not null)
            throw new ConflictException("An account with this email already exists.");

        var user = new ApplicationUser
        {
            Id             = Guid.NewGuid(),
            UserName       = email,
            Email          = email,
            EmailConfirmed = true
        };

        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        try
        {
            var createResult = await userManager.CreateAsync(user, request.Password);
            if (!createResult.Succeeded)
                throw new BadRequestException(
                    "Failed to create account.",
                    [..createResult.Errors.Select(e => e.Description)]);

            var roleResult = await userManager.AddToRoleAsync(user, request.Role);
            if (!roleResult.Succeeded)
                throw new BadRequestException(
                    "Failed to assign role.",
                    [..roleResult.Errors.Select(e => e.Description)]);

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

            var otpRows = await db.OtpCodes
                .Where(o => o.Email == email)
                .ToListAsync(ct);
            db.OtpCodes.RemoveRange(otpRows);
            await db.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);
        }
        catch
        {
            await transaction.RollbackAsync(ct);
            throw;
        }

        return new AuthResponse(
            AccessToken:     tokenService.GenerateToken(user.Id, user.Email!, request.Role),
            ExpiresAt:       tokenService.TokenExpiresAt(),
            UserId:          user.Id,
            Email:           user.Email!,
            Role:            request.Role,
            IsEmailVerified: true,
            IsProfileSetup:  false);
    }
}
