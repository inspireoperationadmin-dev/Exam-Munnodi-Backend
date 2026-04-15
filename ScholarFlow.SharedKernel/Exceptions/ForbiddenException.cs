namespace ScholarFlow.SharedKernel.Exceptions;

public sealed class ForbiddenException(string message)
    : AppException("FORBIDDEN", message, 403);
