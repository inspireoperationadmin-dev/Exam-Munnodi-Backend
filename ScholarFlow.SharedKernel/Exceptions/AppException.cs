namespace ScholarFlow.SharedKernel.Exceptions;

/// <summary>
/// Base class for all domain/application exceptions that map to HTTP error responses.
/// </summary>
public abstract class AppException(string code, string message, int statusCode)
    : Exception(message)
{
    public string Code       { get; } = code;
    public int    StatusCode { get; } = statusCode;
}
