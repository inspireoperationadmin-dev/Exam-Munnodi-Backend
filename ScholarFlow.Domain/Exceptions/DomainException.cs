namespace ScholarFlow.Domain.Exceptions;

/// <summary>
/// Thrown when a domain invariant or business rule is violated.
/// Handlers catch this and return a 400 Bad Request.
/// </summary>
public sealed class DomainException : Exception
{
    public DomainException(string message) : base(message) { }
}
