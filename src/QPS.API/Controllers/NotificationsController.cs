using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QPS.Application.Common.Models;
using QPS.Application.Features.Notifications.Commands;
using QPS.Application.Features.Notifications.DTOs;
using QPS.Application.Features.Notifications.Queries;

namespace QPS.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class NotificationsController : ControllerBase
{
    private readonly ISender _sender;

    public NotificationsController(ISender sender) => _sender = sender;

    [HttpGet]
    public async Task<ActionResult<List<NotificationDto>>> GetNotifications(
        [FromQuery] bool? isRead = null,
        CancellationToken cancellationToken = default)
    {
        var result = await _sender.Send(new GetNotificationsQuery(null, isRead), cancellationToken);
        return Ok(result);
    }

    [HttpPut("{id}/read")]
    public async Task<ActionResult<Result>> MarkAsRead(
        Guid id, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new MarkAsReadCommand(id), cancellationToken);
        if (!result.IsSuccess) return NotFound(result);
        return Ok(result);
    }

    [HttpPut("read-all")]
    public async Task<ActionResult<Result>> MarkAllAsRead(
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new MarkAllAsReadCommand(), cancellationToken);
        return Ok(result);
    }
}