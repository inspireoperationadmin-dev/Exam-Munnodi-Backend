using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ScholarFlow.Domain.Enums;
using ScholarFlow.Modules.Examination.Commands.EndExamSession;
using ScholarFlow.Modules.Examination.Commands.FlagQuestion;
using ScholarFlow.Modules.Examination.Commands.GeneratePersonalizedExam;
using ScholarFlow.Modules.Examination.Commands.StartExamSession;
using ScholarFlow.Modules.Examination.Commands.SubmitAnswer;
using ScholarFlow.Modules.Examination.Queries.GetAvailablePapers;
using ScholarFlow.Modules.Examination.Queries.GetMySessions;
using ScholarFlow.Modules.Examination.Queries.GetPaperQuestionsForExam;
using ScholarFlow.Modules.Examination.Queries.GetSessionDetail;
using ScholarFlow.Modules.Examination.Queries.GetSessionReview;
using ScholarFlow.Modules.Examination.Queries.HasCompletedTest;

namespace ScholarFlow.WebAPI.Controllers;

[Route("api/examination")]
[Authorize(Roles = AppRole.Student)]
public sealed class ExaminationController(IMediator mediator) : ControllerBase
{
    // ── Papers ────────────────────────────────────────────────────────────────

    [HttpGet("papers")]
    public async Task<IActionResult> GetAvailablePapers(
        [FromQuery] Guid? subjectId,
        [FromQuery] string? type,
        [FromQuery] string? medium,
        [FromQuery] int? year,
        CancellationToken ct)
        => Ok(await mediator.Send(new GetAvailablePapersQuery(subjectId, type, medium, year), ct));

    [HttpGet("papers/{id:guid}/questions")]
    public async Task<IActionResult> GetPaperQuestions(Guid id, CancellationToken ct)
        => Ok(await mediator.Send(new GetPaperQuestionsForExamQuery(id), ct));

    // ── Session Lifecycle ─────────────────────────────────────────────────────

    [HttpPost("sessions/start")]
    public async Task<IActionResult> StartSession([FromBody] StartExamSessionCommand command, CancellationToken ct)
        => Ok(await mediator.Send(command, ct));

    [HttpGet("sessions/has-completed/{paperId:guid}")]
    public async Task<IActionResult> HasCompletedTest(Guid paperId, CancellationToken ct)
        => Ok(await mediator.Send(new HasCompletedTestQuery(paperId), ct));

    [HttpPost("sessions/generate")]
    public async Task<IActionResult> GeneratePersonalizedExam([FromBody] GeneratePersonalizedExamCommand command, CancellationToken ct)
        => Ok(await mediator.Send(command, ct));

    [HttpPost("sessions/{id:guid}/answer")]
    public async Task<IActionResult> SubmitAnswer(
        Guid id,
        [FromBody] SubmitAnswerRequest request,
        CancellationToken ct)
    {
        await mediator.Send(new SubmitAnswerCommand(id, request.QuestionId, request.SelectedOptionId, request.TimeSpentSeconds), ct);
        return Ok();
    }

    [HttpPost("sessions/{id:guid}/flag")]
    public async Task<IActionResult> FlagQuestion(
        Guid id,
        [FromBody] FlagQuestionRequest request,
        CancellationToken ct)
    {
        await mediator.Send(new FlagQuestionCommand(id, request.QuestionId, request.Flagged), ct);
        return Ok();
    }

    [HttpPost("sessions/{id:guid}/end")]
    public async Task<IActionResult> EndSession(Guid id, CancellationToken ct)
        => Ok(await mediator.Send(new EndExamSessionCommand(id), ct));

    // ── History & Results ─────────────────────────────────────────────────────

    [HttpGet("sessions")]
    public async Task<IActionResult> GetMySessions(
        [FromQuery] Guid? paperId,
        [FromQuery] bool? isPractice,
        CancellationToken ct)
        => Ok(await mediator.Send(new GetMySessionsQuery(paperId, isPractice), ct));

    [HttpGet("sessions/{id:guid}")]
    public async Task<IActionResult> GetSessionDetail(Guid id, CancellationToken ct)
        => Ok(await mediator.Send(new GetSessionDetailQuery(id), ct));

    [HttpGet("sessions/{id:guid}/review")]
    public async Task<IActionResult> GetSessionReview(Guid id, CancellationToken ct)
        => Ok(await mediator.Send(new GetSessionReviewQuery(id), ct));
}

// ── Request body types ────────────────────────────────────────────────────────

public sealed record SubmitAnswerRequest(
    Guid QuestionId,
    Guid? SelectedOptionId,
    int TimeSpentSeconds);

public sealed record FlagQuestionRequest(
    Guid QuestionId,
    bool Flagged);
