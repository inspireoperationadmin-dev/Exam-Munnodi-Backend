namespace ScholarFlow.SharedKernel.Exceptions;

public sealed class TooManyRequestsException(string message)
    : AppException("TOO_MANY_REQUESTS", message, 429);
