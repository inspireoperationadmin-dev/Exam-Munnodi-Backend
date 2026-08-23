namespace ScholarFlow.SharedKernel.Exceptions;

public sealed class SessionStartConflictException(
    string code,
    string message,
    IReadOnlyList<Guid> sessionIds,
    IReadOnlyList<string> allowedActions)
    : AppException(code, message, 409)
{
    public IReadOnlyList<Guid> SessionIds { get; } = sessionIds;
    public IReadOnlyList<string> AllowedActions { get; } = allowedActions;
}
