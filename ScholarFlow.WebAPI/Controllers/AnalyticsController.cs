using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ScholarFlow.Domain.Enums;
using ScholarFlow.Modules.Analytics.Queries.GetExamHistory;
using ScholarFlow.Modules.Analytics.Queries.GetSubjectPerformance;
using ScholarFlow.Modules.Analytics.Queries.GetSubTopicPerformance;
using ScholarFlow.Modules.Analytics.Queries.GetTopicPerformance;

namespace ScholarFlow.WebAPI.Controllers;

[Route("api/analytics")]
[Authorize(Roles = AppRole.Student)]
public sealed class AnalyticsController(IMediator mediator) : ControllerBase
{
    [HttpGet("subjects")]
    public async Task<IActionResult> GetSubjectPerformance(CancellationToken ct)
        => Ok(await mediator.Send(new GetSubjectPerformanceQuery(), ct));

    [HttpGet("subjects/{subjectId:guid}/subtopics")]
    public async Task<IActionResult> GetSubTopicPerformance(Guid subjectId, CancellationToken ct)
        => Ok(await mediator.Send(new GetSubTopicPerformanceQuery(subjectId), ct));

    [HttpGet("subjects/{subjectId:guid}/topics")]
    public async Task<IActionResult> GetTopicPerformance(Guid subjectId, CancellationToken ct)
        => Ok(await mediator.Send(new GetTopicPerformanceQuery(subjectId), ct));

    [HttpGet("subjects/{subjectId:guid}/history")]
    public async Task<IActionResult> GetExamHistory(Guid subjectId, CancellationToken ct)
        => Ok(await mediator.Send(new GetExamHistoryQuery(subjectId), ct));
}
