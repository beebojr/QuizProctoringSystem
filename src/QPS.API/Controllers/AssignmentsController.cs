using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QPS.Application.Common.Interfaces;
using QPS.Application.Common.Models;
using QPS.Application.Features.Assignments.Commands;
using QPS.Application.Features.Assignments.DTOs;
using QPS.Application.Features.Assignments.Queries;
using QPS.Domain.Entities;

namespace QPS.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class AssignmentsController : ControllerBase
{
    private readonly ISender _sender;
    private readonly ICurrentUserService _currentUserService;

    public AssignmentsController(ISender sender, ICurrentUserService currentUserService)
    {
        _sender = sender;
        _currentUserService = currentUserService;
    }

    [HttpGet]
    public async Task<ActionResult<List<AssignmentDto>>> GetAssignments(
        [FromQuery] Guid? quizId = null,
        [FromQuery] Guid? userId = null,
        CancellationToken cancellationToken = default)
    {
        var result = await _sender.Send(new GetAssignmentsQuery(quizId, userId), cancellationToken);
        return Ok(result);
    }

    [HttpPost("auto-assign")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<Result<List<AssignmentDto>>>> AutoAssign(
        AutoAssignCommand command, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(command, cancellationToken);
        if (!result.IsSuccess) return BadRequest(result);
        return Ok(result);
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<Result<ProctorAssignment>>> ManualAssign(
        ManualAssignCommand command, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(command, cancellationToken);
        if (!result.IsSuccess) return BadRequest(result);
        return Ok(result);
    }

    [HttpGet("my")]
    public async Task<ActionResult<List<AssignmentDto>>> GetMyAssignments(
        CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId;
        if (userId == null) return Unauthorized();

        var result = await _sender.Send(new GetAssignmentsQuery(UserId: userId.Value), cancellationToken);
        return Ok(result);
    }
}