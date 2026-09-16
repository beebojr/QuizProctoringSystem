using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QPS.Application.Common.Models;
using QPS.Application.Features.Semesters.Commands;
using QPS.Application.Features.Semesters.DTOs;
using QPS.Application.Features.Semesters.Queries;

namespace QPS.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class SemestersController : ControllerBase
{
    private readonly ISender _sender;

    public SemestersController(ISender sender) => _sender = sender;

    [HttpGet]
    public async Task<ActionResult<List<SemesterDto>>> GetSemesters(
        CancellationToken cancellationToken = default)
    {
        var result = await _sender.Send(new GetSemestersQuery(), cancellationToken);
        return Ok(result);
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<Result<SemesterDto>>> CreateSemester(
        CreateSemesterCommand command, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(command, cancellationToken);
        if (!result.IsSuccess) return BadRequest(result);
        return CreatedAtAction(nameof(GetSemesters), result);
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<Result<SemesterDto>>> UpdateSemester(
        Guid id, UpdateSemesterCommand command, CancellationToken cancellationToken)
    {
        if (id != command.Id) return BadRequest("ID mismatch");
        var result = await _sender.Send(command, cancellationToken);
        if (!result.IsSuccess) return NotFound(result);
        return Ok(result);
    }
}
