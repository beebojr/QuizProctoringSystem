using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QPS.Application.Features.Excuses;

namespace QPS.API.Controllers;

[Authorize]
public class ExcusesController : ApiControllerBase
{
    [HttpGet]
    public async Task<ActionResult<List<ExcuseDto>>> Get([FromQuery] Guid? userId, [FromQuery] bool activeOnly = false, CancellationToken ct = default) =>
        Ok(await Sender.Send(new GetExcusesQuery(userId, activeOnly), ct));

    [HttpPost]
    public async Task<ActionResult> Create(CreateExcuseCommand command, CancellationToken ct) =>
        FromResult(await Sender.Send(command, ct));

    [HttpDelete("{id:guid}")]
    public async Task<ActionResult> Delete(Guid id, CancellationToken ct) =>
        FromResult(await Sender.Send(new DeleteExcuseCommand(id), ct));
}
