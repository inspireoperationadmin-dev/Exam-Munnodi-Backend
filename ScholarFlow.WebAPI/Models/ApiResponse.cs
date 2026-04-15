namespace ScholarFlow.WebAPI.Models;

public sealed record ApiResponse(
    bool       Success,
    object?    Data,
    ApiError?  Error = null)
{
    public static ApiResponse Ok(object? data)    => new(true,  data, null);
    public static ApiResponse Fail(ApiError error) => new(false, null, error);
}

public sealed record ApiError(
    string                  Code,
    string                  Message,
    IReadOnlyList<string>?  Details = null);
