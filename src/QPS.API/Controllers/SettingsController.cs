using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QPS.Application.Features.Settings;

namespace QPS.API.Controllers;

[Authorize]
public class SettingsController : ApiControllerBase
{
    [HttpGet]
    public async Task<ActionResult<SettingsDto>> Get(CancellationToken ct) => Ok(await Sender.Send(new GetSettingsQuery(), ct));

    [HttpPut]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult> Update(UpdateSettingsCommand command, CancellationToken ct) =>
        FromResult(await Sender.Send(command, ct));
}
