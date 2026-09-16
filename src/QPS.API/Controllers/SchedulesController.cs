using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QPS.Application.Common.Models;
using QPS.Application.Features.Schedules.Commands;
using QPS.Application.Features.Schedules.DTOs;
using QPS.Application.Features.Schedules.Queries;

namespace QPS.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class SchedulesController : ControllerBase
{
    private readonly ISender _sender;

    public SchedulesController(ISender sender) => _sender = sender;

    [HttpGet]
    public async Task<ActionResult<List<ScheduleSlotDto>>> GetSchedules(
        [FromQuery] Guid? userId = null,
        [FromQuery] Guid? semesterId = null,
        CancellationToken cancellationToken = default)
    {
        var result = await _sender.Send(new GetSchedulesQuery(userId, semesterId), cancellationToken);
        return Ok(result);
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<Result<ScheduleSlotDto>>> CreateSlot(
        CreateScheduleSlotCommand command, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(command, cancellationToken);
        if (!result.IsSuccess) return BadRequest(result);
        return Ok(result);
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<Result>> DeleteSlot(
        Guid id, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new DeleteScheduleSlotCommand(id), cancellationToken);
        if (!result.IsSuccess) return NotFound(result);
        return Ok(result);
    }
}
