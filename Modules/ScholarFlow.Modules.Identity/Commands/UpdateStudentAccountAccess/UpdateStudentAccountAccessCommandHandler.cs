using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ScholarFlow.Domain.Entities;
using ScholarFlow.Domain.Enums;
using ScholarFlow.Infrastructure.Persistence;
using ScholarFlow.Infrastructure.Services;
using ScholarFlow.SharedKernel.Exceptions;

namespace ScholarFlow.Modules.Identity.Commands.UpdateStudentAccountAccess;

public sealed class UpdateStudentAccountAccessCommandHandler(
    UserManager<ApplicationUser> userManager,
    ApplicationDbContext db,
    IEmailService emailService,
    ILogger<UpdateStudentAccountAccessCommandHandler> logger)
    : IRequestHandler<UpdateStudentAccountAccessCommand, StudentAccountAccessResult>
{
    private static readonly DateTimeOffset IndefiniteLockoutEnd = DateTimeOffset.MaxValue;

    public async Task<StudentAccountAccessResult> Handle(
        UpdateStudentAccountAccessCommand request,
        CancellationToken ct)
    {
        var user = await userManager.FindByIdAsync(request.StudentId.ToString())
            ?? throw new NotFoundException("Student account not found.");

        if (!await userManager.IsInRoleAsync(user, AppRole.Student))
            throw new NotFoundException("Student account not found.");

        var restrictedUntil = request.Action switch
        {
            StudentAccountAccessAction.Suspend => request.SuspendedUntil,
            StudentAccountAccessAction.Deactivate => IndefiniteLockoutEnd,
            StudentAccountAccessAction.Reactivate => null,
            _ => throw new BadRequestException("Unsupported account access action.")
        };

        user.LockoutEnabled = true;
        user.LockoutEnd = restrictedUntil;
        if (request.Action == StudentAccountAccessAction.Reactivate)
            user.AccessFailedCount = 0;

        EnsureSucceeded(
            await userManager.UpdateAsync(user),
            "Could not update account access.");

        var fullName = await db.StudentProfiles
            .AsNoTracking()
            .Where(profile => profile.UserId == user.Id)
            .Select(profile => profile.FullName)
            .FirstOrDefaultAsync(ct) ?? user.Email ?? "Student";

        var accountStatus = request.Action switch
        {
            StudentAccountAccessAction.Suspend => "Suspended",
            StudentAccountAccessAction.Deactivate => "Deactivated",
            _ => "Active"
        };

        var emailSent = true;
        try
        {
            await emailService.SendAccountAccessChangedAsync(
                user.Email!,
                fullName,
                accountStatus,
                request.Action == StudentAccountAccessAction.Suspend ? restrictedUntil : null,
                ct);
        }
        catch (Exception exception)
        {
            emailSent = false;
            logger.LogError(
                exception,
                "Account access changed to {AccountStatus} for student {StudentId}, but email delivery failed.",
                accountStatus,
                user.Id);
        }

        return new StudentAccountAccessResult(
            user.Id,
            accountStatus,
            request.Action == StudentAccountAccessAction.Reactivate ? null : restrictedUntil,
            emailSent);
    }

    private static void EnsureSucceeded(IdentityResult result, string message)
    {
        if (!result.Succeeded)
            throw new BadRequestException(message, [.. result.Errors.Select(error => error.Description)]);
    }
}
