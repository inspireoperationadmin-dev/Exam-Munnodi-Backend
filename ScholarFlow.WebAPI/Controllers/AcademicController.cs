using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ScholarFlow.Domain.Enums;
using ScholarFlow.Modules.Academic.Commands.Papers.AddExplanation;
using ScholarFlow.Modules.Academic.Commands.Papers.AddQuestionToPaper;
using ScholarFlow.Modules.Academic.Commands.Papers.CreatePaper;
using ScholarFlow.Modules.Academic.Commands.Papers.DeleteExplanation;
using ScholarFlow.Modules.Academic.Commands.Papers.DeletePaper;
using ScholarFlow.Modules.Academic.Commands.Papers.DeleteQuestion;
using ScholarFlow.Modules.Academic.Commands.Papers.TogglePaperVisibility;
using ScholarFlow.Modules.Academic.Commands.Papers.UpdateExplanation;
using ScholarFlow.Modules.Academic.Commands.Papers.UpdateOption;
using ScholarFlow.Modules.Academic.Commands.Papers.UpdatePaper;
using ScholarFlow.Modules.Academic.Commands.Papers.UpdateQuestion;
using ScholarFlow.Modules.Academic.Commands.Streams.CreateStream;
using ScholarFlow.Modules.Academic.Commands.Streams.DeleteStream;
using ScholarFlow.Modules.Academic.Commands.Streams.UpdateStream;
using ScholarFlow.Modules.Academic.Commands.Subjects.AssignSubjectToStream;
using ScholarFlow.Modules.Academic.Commands.Subjects.CreateSubject;
using ScholarFlow.Modules.Academic.Commands.Subjects.DeleteSubject;
using ScholarFlow.Modules.Academic.Commands.Subjects.RemoveSubjectFromStream;
using ScholarFlow.Modules.Academic.Commands.Subjects.UpdateSubject;
using ScholarFlow.Modules.Academic.Commands.Topics.CreateSubTopic;
using ScholarFlow.Modules.Academic.Commands.Topics.CreateTopic;
using ScholarFlow.Modules.Academic.Commands.Topics.DeleteSubTopic;
using ScholarFlow.Modules.Academic.Commands.Topics.DeleteTopic;
using ScholarFlow.Modules.Academic.Commands.Topics.UpdateSubTopic;
using ScholarFlow.Modules.Academic.Commands.Topics.UpdateTopic;
using ScholarFlow.Modules.Academic.Queries.GetAcademicTree;
using ScholarFlow.Modules.Academic.Queries.GetAllStreams;
using ScholarFlow.Modules.Academic.Queries.GetAllSubjects;
using ScholarFlow.Modules.Academic.Queries.GetPaperDetail;
using ScholarFlow.Modules.Academic.Queries.GetPaperQuestions;
using ScholarFlow.Modules.Academic.Queries.GetPapers;
using ScholarFlow.Modules.Academic.Queries.GetSubjectDetail;
using ScholarFlow.Modules.Academic.Queries.GetTopicsBySubject;

namespace ScholarFlow.WebAPI.Controllers;

[ApiController]
[Route("api/academic")]
[Authorize]
public sealed class AcademicController(IMediator mediator) : ControllerBase
{
    // ── Full Tree ─────────────────────────────────────────────────────────────

    [HttpGet("tree")]
    public async Task<IActionResult> GetTree(CancellationToken ct)
        => Ok(await mediator.Send(new GetAcademicTreeQuery(), ct));

    // ── Streams ───────────────────────────────────────────────────────────────

    [HttpGet("streams")]
    public async Task<IActionResult> GetStreams(CancellationToken ct)
        => Ok(await mediator.Send(new GetAllStreamsQuery(), ct));

    [HttpPost("streams")]
    [Authorize(Roles = AppRole.Admin + "," + AppRole.SuperAdmin)]
    public async Task<IActionResult> CreateStream([FromBody] CreateStreamCommand command, CancellationToken ct)
        => Ok(await mediator.Send(command, ct));

    [HttpPut("streams/{id:guid}")]
    [Authorize(Roles = AppRole.Admin + "," + AppRole.SuperAdmin)]
    public async Task<IActionResult> UpdateStream(Guid id, [FromBody] UpdateStreamCommand command, CancellationToken ct)
    {
        await mediator.Send(command with { Id = id }, ct);
        return Ok();
    }

    [HttpDelete("streams/{id:guid}")]
    [Authorize(Roles = AppRole.Admin+ "," + AppRole.SuperAdmin)]
    public async Task<IActionResult> DeleteStream(Guid id, CancellationToken ct)
    {
        await mediator.Send(new DeleteStreamCommand(id), ct);
        return Ok();
    }

    // ── Subjects ──────────────────────────────────────────────────────────────

    [HttpGet("subjects")]
    public async Task<IActionResult> GetSubjects([FromQuery] Guid? streamId, CancellationToken ct)
        => Ok(await mediator.Send(new GetAllSubjectsQuery(streamId), ct));

    [HttpGet("subjects/{id:guid}")]
    public async Task<IActionResult> GetSubjectDetail(Guid id, CancellationToken ct)
        => Ok(await mediator.Send(new GetSubjectDetailQuery(id), ct));

    [HttpPost("subjects")]
    [Authorize(Roles = AppRole.Admin + "," + AppRole.SuperAdmin)]
    public async Task<IActionResult> CreateSubject([FromBody] CreateSubjectCommand command, CancellationToken ct)
        => Ok(await mediator.Send(command, ct));

    [HttpPut("subjects/{id:guid}")]
    [Authorize(Roles = AppRole.Admin + "," + AppRole.SuperAdmin)]
    public async Task<IActionResult> UpdateSubject(Guid id, [FromBody] UpdateSubjectCommand command, CancellationToken ct)
    {
        await mediator.Send(command with { Id = id }, ct);
        return Ok();
    }

    [HttpDelete("subjects/{id:guid}")]
    [Authorize(Roles = AppRole.Admin + "," + AppRole.SuperAdmin)]
    public async Task<IActionResult> DeleteSubject(Guid id, CancellationToken ct)
    {
        await mediator.Send(new DeleteSubjectCommand(id), ct);
        return Ok();
    }

    [HttpPost("subjects/{id:guid}/streams/{streamId:guid}")]
    [Authorize(Roles = AppRole.Admin + "," + AppRole.SuperAdmin)]
    public async Task<IActionResult> AssignSubjectToStream(Guid id, Guid streamId, CancellationToken ct)
    {
        await mediator.Send(new AssignSubjectToStreamCommand(id, streamId), ct);
        return Ok();
    }

    [HttpDelete("subjects/{id:guid}/streams/{streamId:guid}")]
    [Authorize(Roles = AppRole.Admin + "," + AppRole.SuperAdmin)]
    public async Task<IActionResult> RemoveSubjectFromStream(Guid id, Guid streamId, CancellationToken ct)
    {
        await mediator.Send(new RemoveSubjectFromStreamCommand(id, streamId), ct);
        return Ok();
    }

    // ── Topics ────────────────────────────────────────────────────────────────

    [HttpGet("topics")]
    public async Task<IActionResult> GetTopics([FromQuery] Guid subjectId, CancellationToken ct)
        => Ok(await mediator.Send(new GetTopicsBySubjectQuery(subjectId), ct));

    [HttpPost("topics")]
    [Authorize(Roles = AppRole.Admin + "," + AppRole.SuperAdmin)]
    public async Task<IActionResult> CreateTopic([FromBody] CreateTopicCommand command, CancellationToken ct)
        => Ok(await mediator.Send(command, ct));

    [HttpPut("topics/{id:guid}")]
    [Authorize(Roles = AppRole.Admin + "," + AppRole.SuperAdmin)]
    public async Task<IActionResult> UpdateTopic(Guid id, [FromBody] UpdateTopicCommand command, CancellationToken ct)
    {
        await mediator.Send(command with { Id = id }, ct);
        return Ok();
    }

    [HttpDelete("topics/{id:guid}")]
    [Authorize(Roles = AppRole.Admin + "," + AppRole.SuperAdmin)]
    public async Task<IActionResult> DeleteTopic(Guid id, CancellationToken ct)
    {
        await mediator.Send(new DeleteTopicCommand(id), ct);
        return Ok();
    }

    // ── SubTopics ─────────────────────────────────────────────────────────────

    [HttpPost("topics/{topicId:guid}/subtopics")]
    [Authorize(Roles = AppRole.Admin + "," + AppRole.SuperAdmin)]
    public async Task<IActionResult> CreateSubTopic(Guid topicId, [FromBody] CreateSubTopicCommand command, CancellationToken ct)
        => Ok(await mediator.Send(command with { TopicId = topicId }, ct));

    [HttpPut("subtopics/{id:guid}")]
    [Authorize(Roles = AppRole.Admin + "," + AppRole.SuperAdmin)]
    public async Task<IActionResult> UpdateSubTopic(Guid id, [FromBody] UpdateSubTopicCommand command, CancellationToken ct)
    {
        await mediator.Send(command with { Id = id }, ct);
        return Ok();
    }

    [HttpDelete("subtopics/{id:guid}")]
    [Authorize(Roles = AppRole.Admin + "," + AppRole.SuperAdmin)]
    public async Task<IActionResult> DeleteSubTopic(Guid id, CancellationToken ct)
    {
        await mediator.Send(new DeleteSubTopicCommand(id), ct);
        return Ok();
    }

    // ── Papers ────────────────────────────────────────────────────────────────

    [HttpGet("papers")]
    public async Task<IActionResult> GetPapers(
        [FromQuery] Guid? subjectId,
        [FromQuery] PaperType? type,
        [FromQuery] PaperMedium? medium,
        [FromQuery] int? year,
        CancellationToken ct)
        => Ok(await mediator.Send(new GetPapersQuery(subjectId, type, medium, year), ct));

    [HttpGet("papers/{id:guid}")]
    public async Task<IActionResult> GetPaperDetail(Guid id, CancellationToken ct)
        => Ok(await mediator.Send(new GetPaperDetailQuery(id), ct));

    [HttpPost("papers")]
    [Authorize(Roles = $"{AppRole.Admin},{AppRole.Teacher},{AppRole.SuperAdmin}")]
    public async Task<IActionResult> CreatePaper([FromBody] CreatePaperCommand command, CancellationToken ct)
        => Ok(await mediator.Send(command, ct));

    [HttpPut("papers/{id:guid}")]
    [Authorize(Roles = $"{AppRole.Admin},{AppRole.Teacher},{AppRole.SuperAdmin}")]
    public async Task<IActionResult> UpdatePaper(Guid id, [FromBody] UpdatePaperCommand command, CancellationToken ct)
    {
        await mediator.Send(command with { Id = id }, ct);
        return Ok();
    }

    [HttpDelete("papers/{id:guid}")]
    [Authorize(Roles = $"{AppRole.Admin},{AppRole.Teacher},{AppRole.SuperAdmin}")]
    public async Task<IActionResult> DeletePaper(Guid id, CancellationToken ct)
    {
        await mediator.Send(new DeletePaperCommand(id), ct);
        return Ok();
    }

    [HttpPatch("papers/{id:guid}/visibility")]
    [Authorize(Roles = $"{AppRole.Admin},{AppRole.Teacher},{AppRole.SuperAdmin}")]
    public async Task<IActionResult> ToggleVisibility(Guid id, [FromBody] TogglePaperVisibilityCommand command, CancellationToken ct)
    {
        await mediator.Send(command with { Id = id }, ct);
        return Ok();
    }

    // ── Questions ─────────────────────────────────────────────────────────────

    [HttpGet("papers/{paperId:guid}/questions")]
    public async Task<IActionResult> GetQuestions(Guid paperId, CancellationToken ct)
        => Ok(await mediator.Send(new GetPaperQuestionsQuery(paperId), ct));

    [HttpPost("papers/{paperId:guid}/questions")]
    [Authorize(Roles = $"{AppRole.Admin},{AppRole.Teacher},{AppRole.SuperAdmin}")]
    public async Task<IActionResult> AddQuestion(Guid paperId, [FromBody] AddQuestionToPaperCommand command, CancellationToken ct)
        => Ok(await mediator.Send(command with { PaperId = paperId }, ct));

    [HttpPut("questions/{id:guid}")]
    [Authorize(Roles = $"{AppRole.Admin},{AppRole.Teacher},{AppRole.SuperAdmin}")]
    public async Task<IActionResult> UpdateQuestion(Guid id, [FromBody] UpdateQuestionCommand command, CancellationToken ct)
    {
        await mediator.Send(command with { Id = id }, ct);
        return Ok();
    }

    [HttpDelete("questions/{id:guid}")]
    [Authorize(Roles = $"{AppRole.Admin},{AppRole.Teacher},{AppRole.SuperAdmin}")]
    public async Task<IActionResult> DeleteQuestion(Guid id, CancellationToken ct)
    {
        await mediator.Send(new DeleteQuestionCommand(id), ct);
        return Ok();
    }

    // ── Options ───────────────────────────────────────────────────────────────

    [HttpPut("options/{id:guid}")]
    [Authorize(Roles = $"{AppRole.Admin},{AppRole.Teacher},{AppRole.SuperAdmin}")]
    public async Task<IActionResult> UpdateOption(Guid id, [FromBody] UpdateOptionCommand command, CancellationToken ct)
    {
        await mediator.Send(command with { Id = id }, ct);
        return Ok();
    }

    // ── Explanations ──────────────────────────────────────────────────────────

    [HttpPost("questions/{questionId:guid}/explanation")]
    [Authorize(Roles = $"{AppRole.Admin},{AppRole.Teacher},{AppRole.SuperAdmin}")]
    public async Task<IActionResult> AddExplanation(Guid questionId, [FromBody] AddExplanationCommand command, CancellationToken ct)
        => Ok(await mediator.Send(command with { QuestionId = questionId }, ct));

    [HttpPut("explanations/{id:guid}")]
    [Authorize(Roles = $"{AppRole.Admin},{AppRole.Teacher},{AppRole.SuperAdmin}")]
    public async Task<IActionResult> UpdateExplanation(Guid id, [FromBody] UpdateExplanationCommand command, CancellationToken ct)
    {
        await mediator.Send(command with { Id = id }, ct);
        return Ok();
    }

    [HttpDelete("explanations/{id:guid}")]
    [Authorize(Roles = $"{AppRole.Admin},{AppRole.Teacher},{AppRole.SuperAdmin}")]
    public async Task<IActionResult> DeleteExplanation(Guid id, CancellationToken ct)
    {
        await mediator.Send(new DeleteExplanationCommand(id), ct);
        return Ok();
    }
}
