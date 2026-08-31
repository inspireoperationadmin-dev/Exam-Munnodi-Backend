using MediatR;

namespace ScholarFlow.Modules.Identity.Commands.DeleteStudentAccount;

public sealed record DeleteStudentAccountCommand(
    Guid StudentId,
    string ConfirmationEmail)
    : IRequest<DeleteStudentAccountResult>;

public sealed record DeleteStudentAccountResult(
    Guid StudentId,
    string Email,
    bool EmailNotificationSent);
