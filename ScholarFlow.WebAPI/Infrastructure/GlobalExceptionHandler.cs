using FluentValidation;
using Microsoft.AspNetCore.Diagnostics;
using ScholarFlow.SharedKernel.Exceptions;
using ScholarFlow.WebAPI.Models;

namespace ScholarFlow.WebAPI.Infrastructure;

public sealed class GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger)
    : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception   exception,
        CancellationToken ct)
    {
        var (statusCode, error) = exception switch
        {
            SessionStartConflictException ex => (
                ex.StatusCode,
                new ApiError(
                    ex.Code,
                    ex.Message,
                    Metadata: new Dictionary<string, object?>
                    {
                        ["sessionId"] = ex.SessionIds.Count == 1 ? ex.SessionIds[0] : null,
                        ["sessionIds"] = ex.SessionIds,
                        ["allowedActions"] = ex.AllowedActions
                    })),

            ValidationException ex => (
                StatusCodes.Status400BadRequest,
                new ApiError(
                    "VALIDATION_ERROR",
                    "One or more validation errors occurred.",
                    [..ex.Errors.Select(e => e.ErrorMessage)])),

            BadRequestException ex => (
                ex.StatusCode,
                new ApiError(ex.Code, ex.Message, ex.Details)),

            AppException ex => (
                ex.StatusCode,
                new ApiError(ex.Code, ex.Message)),

            _ => (
                StatusCodes.Status500InternalServerError,
                new ApiError("INTERNAL_ERROR", "An unexpected error occurred."))
        };

        if (statusCode == StatusCodes.Status500InternalServerError)
            logger.LogError(exception, "Unhandled exception: {Message}", exception.Message);

        httpContext.Response.StatusCode  = statusCode;
        httpContext.Response.ContentType = "application/json";

        await httpContext.Response.WriteAsJsonAsync(ApiResponse.Fail(error), ct);
        return true;
    }
}
