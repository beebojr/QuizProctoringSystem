using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QPS.Application.Features.Rooms;

namespace QPS.API.Controllers;

[Authorize(Roles = "Admin")]
public class RoomsController : ApiControllerBase
{
    [HttpGet]
    public async Task<ActionResult<List<RoomDto>>> Get(CancellationToken ct) => Ok(await Sender.Send(new GetRoomsQuery(), ct));

    [HttpPost]
    public async Task<ActionResult> Create(SaveRoomCommand command, CancellationToken ct) =>
        FromResult(await Sender.Send(command with { Id = null }, ct));

    [HttpPut("{id:guid}")]
    public async Task<ActionResult> Update(Guid id, SaveRoomCommand command, CancellationToken ct) =>
        FromResult(await Sender.Send(command with { Id = id }, ct));

    [HttpDelete("{id:guid}")]
    public async Task<ActionResult> Delete(Guid id, CancellationToken ct) =>
        FromResult(await Sender.Send(new DeleteRoomCommand(id), ct));
}
