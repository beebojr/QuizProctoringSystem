using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QPS.Application.Features.Reports;

namespace QPS.API.Controllers;

[Authorize]
public class DashboardController : ApiControllerBase
{
    [HttpGet("admin")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<AdminDashboardDto>> Admin([FromQuery] Guid? semesterId, CancellationToken ct) =>
        Ok(await Sender.Send(new GetAdminDashboardQuery(semesterId), ct));

    [HttpGet("ta")]
    [Authorize(Roles = "TA")]
    public async Task<ActionResult<TaDashboardDto>> Ta(CancellationToken ct) =>
        Ok(await Sender.Send(new GetTaDashboardQuery(), ct));
}
