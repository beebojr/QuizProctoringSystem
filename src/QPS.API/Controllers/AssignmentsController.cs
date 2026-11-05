using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QPS.Application.Features.Assignments.Commands;
using QPS.Application.Features.Assignments.DTOs;
using QPS.Application.Features.Assignments.Queries;

namespace QPS.API.Controllers;

[Authorize]
public class AssignmentsController : ApiControllerBase
{
    [HttpGet]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<List<AssignmentDto>>> Get([FromQuery] Guid? semesterId, [FromQuery] Guid? userId,
        [FromQuery] Guid? quizId, CancellationToken ct) =>
        Ok(await Sender.Send(new GetAssignmentsQuery(semesterId, userId, quizId), ct));

    [HttpGet("my")]
    public async Task<ActionResult<List<AssignmentDto>>> Mine([FromQuery] Guid? semesterId, CancellationToken ct) =>
        Ok(await Sender.Send(new GetMyAssignmentsQuery(semesterId), ct));

    [HttpGet("candidates/{locationId:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<CandidatesDto>> Candidates(Guid locationId, CancellationToken ct) =>
        Ok(await Sender.Send(new GetCandidatesQuery(locationId), ct));

    /// <summary>Manual assignment. Returns assigned=false with the broken rules unless force is set.</summary>
    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<AssignOutcomeDto>> Assign(AssignProctorCommand command, CancellationToken ct) =>
        Ok(await Sender.Send(command, ct));

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult> Remove(Guid id, CancellationToken ct) =>
        FromResult(await Sender.Send(new RemoveAssignmentCommand(id), ct));

    [HttpPost("{id:guid}/replace")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ReplaceOutcomeDto>> Replace(Guid id, CancellationToken ct) =>
        Ok(await Sender.Send(new ReplaceAssignmentCommand(id), ct));

    [HttpPost("auto-assign/quiz/{quizId:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult> AutoAssignQuiz(Guid quizId, CancellationToken ct) =>
        FromResult(await Sender.Send(new AutoAssignQuizCommand(quizId), ct));

    [HttpPost("auto-assign/preview")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<BatchPreviewDto>> Preview(AutoAssignPreviewCommand command, CancellationToken ct) =>
        Ok(await Sender.Send(command, ct));

    [HttpPost("auto-assign/commit")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult> Commit(AutoAssignCommitCommand command, CancellationToken ct) =>
        FromResult(await Sender.Send(command, ct));

    [HttpDelete("quiz/{quizId:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult> Clear(Guid quizId, CancellationToken ct) =>
        FromResult(await Sender.Send(new ClearQuizAssignmentsCommand(quizId), ct));
}
