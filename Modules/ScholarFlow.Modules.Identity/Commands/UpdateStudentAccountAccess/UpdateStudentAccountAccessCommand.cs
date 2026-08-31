using MediatR;

namespace ScholarFlow.Modules.Identity.Commands.UpdateStudentAccountAccess;

public enum StudentAccountAccessAction
{
    Suspend,
    Deactivate,
    Reactivate
}

public sealed record UpdateStudentAccountAccessCommand(
    Guid StudentId,
    StudentAccountAccessAction Action,
    DateTimeOffset? SuspendedUntil)
    : IRequest<StudentAccountAccessResult>;

public sealed record StudentAccountAccessResult(
    Guid StudentId,
    string AccountStatus,
    DateTimeOffset? AccessRestrictedUntil,
    bool EmailNotificationSent);
