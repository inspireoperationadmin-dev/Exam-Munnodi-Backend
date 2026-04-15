namespace ScholarFlow.SharedKernel.Exceptions;

public sealed class ConflictException(string message)
    : AppException("CONFLICT", message, 409);
