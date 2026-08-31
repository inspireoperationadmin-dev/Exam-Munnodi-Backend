using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ScholarFlow.Domain.Entities;
using ScholarFlow.Domain.Enums;
using ScholarFlow.Domain.Interfaces;
using ScholarFlow.Infrastructure.Persistence;
using ScholarFlow.Infrastructure.Services;
using ScholarFlow.SharedKernel.Exceptions;

namespace ScholarFlow.Modules.Identity.Commands.DeleteStudentAccount;

public sealed class DeleteStudentAccountCommandHandler(
    UserManager<ApplicationUser> userManager,
    ApplicationDbContext db,
    ICurrentUser currentUser,
    IEmailService emailService,
    ILogger<DeleteStudentAccountCommandHandler> logger)
    : IRequestHandler<DeleteStudentAccountCommand, DeleteStudentAccountResult>
{
    public async Task<DeleteStudentAccountResult> Handle(
        DeleteStudentAccountCommand request,
        CancellationToken ct)
    {
        var user = await userManager.FindByIdAsync(request.StudentId.ToString())
            ?? throw new NotFoundException("Student account not found.");

        if (!await userManager.IsInRoleAsync(user, AppRole.Student))
            throw new NotFoundException("Student account not found.");

        var email = user.Email
            ?? throw new ConflictException("The student account has no email address.");

        if (!string.Equals(
                email.Trim(),
                request.ConfirmationEmail.Trim(),
                StringComparison.OrdinalIgnoreCase))
        {
            throw new BadRequestException(
                "The confirmation email does not match the student account.");
        }

        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        try
        {
            // Remove rows that either restrict another user-owned row or have no user FK.
            await db.StudentStudyActivities
                .IgnoreQueryFilters()
                .Where(row => row.UserId == user.Id)
                .ExecuteDeleteAsync(ct);
            await db.SubscriptionPayments
                .IgnoreQueryFilters()
                .Where(row => row.UserId == user.Id)
                .ExecuteDeleteAsync(ct);
            await db.StudentSubscriptions
                .IgnoreQueryFilters()
                .Where(row => row.UserId == user.Id)
                .ExecuteDeleteAsync(ct);
            await db.StudentSubscriptionUsages
                .IgnoreQueryFilters()
                .Where(row => row.UserId == user.Id)
                .ExecuteDeleteAsync(ct);
            await db.NotificationDevices
                .IgnoreQueryFilters()
                .Where(row => row.UserId == user.Id)
                .ExecuteDeleteAsync(ct);
            await db.StudentNotificationPreferences
                .IgnoreQueryFilters()
                .Where(row => row.UserId == user.Id)
                .ExecuteDeleteAsync(ct);
            await db.ExamSessions
                .IgnoreQueryFilters()
                .Where(row => row.UserId == user.Id)
                .ExecuteDeleteAsync(ct);
            await db.StudentQuestionProgresses
                .IgnoreQueryFilters()
                .Where(row => row.UserId == user.Id)
                .ExecuteDeleteAsync(ct);
            await db.StudentSubjectPerformances
                .IgnoreQueryFilters()
                .Where(row => row.UserId == user.Id)
                .ExecuteDeleteAsync(ct);
            await db.StudentSubTopicPerformances
                .IgnoreQueryFilters()
                .Where(row => row.UserId == user.Id)
                .ExecuteDeleteAsync(ct);
            await db.StudentProfiles
                .IgnoreQueryFilters()
                .Where(row => row.UserId == user.Id)
                .ExecuteDeleteAsync(ct);
            await db.OtpCodes
                .Where(row => row.Email == email)
                .ExecuteDeleteAsync(ct);

            var deleteResult = await userManager.DeleteAsync(user);
            if (!deleteResult.Succeeded)
            {
                throw new BadRequestException(
                    "Could not delete the student account.",
                    [.. deleteResult.Errors.Select(error => error.Description)]);
            }

            await transaction.CommitAsync(ct);
        }
        catch
        {
            await transaction.RollbackAsync(ct);
            throw;
        }

        logger.LogWarning(
            "Student account {StudentId} ({Email}) was permanently deleted by SuperAdmin {AdminId}.",
            user.Id,
            email,
            currentUser.UserId);

        var emailSent = true;
        try
        {
            await emailService.SendAccountDeletedAsync(email, ct);
        }
        catch (Exception exception)
        {
            emailSent = false;
            logger.LogError(
                exception,
                "Student account {StudentId} was deleted, but the deletion email could not be sent.",
                user.Id);
        }

        return new DeleteStudentAccountResult(user.Id, email, emailSent);
    }
}
