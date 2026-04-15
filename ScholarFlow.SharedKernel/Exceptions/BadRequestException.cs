namespace ScholarFlow.SharedKernel.Exceptions;

public sealed class BadRequestException(
    string message,
    IReadOnlyList<string>? details = null)
    : AppException("BAD_REQUEST", message, 400)
{
    public IReadOnlyList<string>? Details { get; } = details;
}
