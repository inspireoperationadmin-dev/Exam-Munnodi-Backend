namespace ScholarFlow.SharedKernel.Exceptions;

public sealed class NotFoundException : AppException
{
    /// <summary>Usage: throw new NotFoundException("Paper", id)</summary>
    public NotFoundException(string resource, object key)
        : base("NOT_FOUND", $"{resource} '{key}' was not found.", 404) { }

    /// <summary>Usage: throw new NotFoundException("Custom message")</summary>
    public NotFoundException(string message)
        : base("NOT_FOUND", message, 404) { }
}
