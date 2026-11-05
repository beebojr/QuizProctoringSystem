using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QPS.Application.Features.Notifications;

namespace QPS.API.Controllers;

[Authorize]
public class NotificationsController : ApiControllerBase
{
    [HttpGet]
    public async Task<ActionResult<NotificationFeedDto>> Get([FromQuery] int take = 50, [FromQuery] bool unreadOnly = false, CancellationToken ct = default) =>
        Ok(await Sender.Send(new GetNotificationsQuery(take, unreadOnly), ct));

    [HttpPut("{id:guid}/read")]
    public async Task<ActionResult> MarkRead(Guid id, CancellationToken ct) =>
        FromResult(await Sender.Send(new MarkAsReadCommand(id), ct));

    [HttpPut("read-all")]
    public async Task<ActionResult> MarkAllRead(CancellationToken ct) =>
        FromResult(await Sender.Send(new MarkAllAsReadCommand(), ct));
}
