using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ScholarFlow.Domain.Enums;
using ScholarFlow.Modules.Examination.Commands.AbandonExamSession;
using ScholarFlow.Modules.Examination.Commands.EndExamSession;
using ScholarFlow.Modules.Examination.Commands.FlagQuestion;
using ScholarFlow.Modules.Examination.Commands.GeneratePersonalizedExam;
using ScholarFlow.Modules.Examination.Commands.StartExamSession;
using ScholarFlow.Modules.Examination.Commands.StartTopicExamSession; // <-- Added namespace import [1]
using ScholarFlow.Modules.Examination.Commands.SubmitAnswer;
using ScholarFlow.Modules.Examination.Queries.GetAvailablePapers;
using ScholarFlow.Modules.Examination.Queries.GetActiveSession;
using ScholarFlow.Modules.Examination.Queries.GetMySessions;
using ScholarFlow.Modules.Examination.Queries.GetPaperQuestionsForExam;
using ScholarFlow.Modules.Examination.Queries.GetSessionDetail;
using ScholarFlow.Modules.Examination.Queries.GetSessionReview;
using ScholarFlow.Modules.Examination.Queries.GetSessionResume;
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

    // New: Handle Topic-Wise / Unit-Wise randomized exam session startup [1]
    [HttpPost("sessions/start-topic")]
    public async Task<IActionResult> StartTopicSession([FromBody] StartTopicExamSessionCommand command, CancellationToken ct)
        => Ok(await mediator.Send(command, ct));

    [HttpGet("sessions/has-completed/{paperId:guid}")]
    public async Task<IActionResult> HasCompletedTest(Guid paperId, CancellationToken ct)
        => Ok(await mediator.Send(new HasCompletedTestQuery(paperId), ct));

    [HttpPost("sessions/generate")]
    public async Task<IActionResult> GeneratePersonalizedExam([FromBody] GeneratePersonalizedExamCommand command, CancellationToken ct)
        => Ok(await mediator.Send(command, ct));

    [HttpGet("sessions/active")]
    public async Task<IActionResult> GetActiveSession([FromQuery] Guid? subjectId, CancellationToken ct)
        => Ok(await mediator.Send(new GetActiveSessionQuery(subjectId), ct));

    [HttpGet("sessions/{id:guid}/resume")]
    public async Task<IActionResult> ResumeSession(Guid id, CancellationToken ct)
        => Ok(await mediator.Send(new GetSessionResumeQuery(id), ct));

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

    // Updated: Accept the list of answers in the request body [1]
    [HttpPost("sessions/{id:guid}/end")]
    public async Task<IActionResult> EndSession(
        Guid id, 
        [FromBody] EndExamSessionRequest request, 
        CancellationToken ct)
        => Ok(await mediator.Send(new EndExamSessionCommand(id, request.Answers), ct));

    [HttpPost("sessions/{id:guid}/abandon")]
    public async Task<IActionResult> AbandonSession(Guid id, CancellationToken ct)
    {
        await mediator.Send(new AbandonExamSessionCommand(id), ct);
        return Ok();
    }

    // ── History & Results ─────────────────────────────────────────────────────

    [HttpGet("sessions")]
    public async Task<IActionResult> GetMySessions(
        [FromQuery] Guid? paperId,
        [FromQuery] ExamMode? mode,
        CancellationToken ct)
        => Ok(await mediator.Send(new GetMySessionsQuery(paperId, mode), ct));

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

// New: Wrap the bulk answers list into a request record [1]
public sealed record EndExamSessionRequest(List<SubmittedAnswerDto> Answers);
