using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using ScholarFlow.WebAPI.Models;

namespace ScholarFlow.WebAPI.Filters;

/// <summary>
/// Wraps all successful action results in a uniform <see cref="ApiResponse"/> envelope.
/// Exception responses are handled separately by <see cref="Infrastructure.GlobalExceptionHandler"/>.
/// </summary>
public sealed class ApiResponseFilter : IActionFilter
{
    public void OnActionExecuting(ActionExecutingContext context) { }

    public void OnActionExecuted(ActionExecutedContext context)
    {
        // Let the exception handler deal with failures
        if (context.Exception is not null) return;

        context.Result = context.Result switch
        {
            OkObjectResult { Value: var data }      => new OkObjectResult(ApiResponse.Ok(data)),
            OkResult                                => new OkObjectResult(ApiResponse.Ok(null)),
            CreatedAtActionResult r                 => new CreatedAtActionResult(
                                                            r.ActionName, r.ControllerName,
                                                            r.RouteValues, ApiResponse.Ok(r.Value)),
            _ => context.Result
        };
    }
}
