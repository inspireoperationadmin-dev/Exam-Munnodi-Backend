namespace ScholarFlow.SharedKernel.Exceptions;

public sealed class UnauthorizedException(string message)
    : AppException("UNAUTHORIZED", message, 401);
