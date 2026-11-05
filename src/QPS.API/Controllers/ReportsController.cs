using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QPS.Application.Features.Reports;

namespace QPS.API.Controllers;

[Authorize(Roles = "Admin")]
public class ReportsController : ApiControllerBase
{
    [HttpGet("tas")]
    public async Task<ActionResult<List<TaSummaryDto>>> Tas([FromQuery] Guid? semesterId, CancellationToken ct) =>
        Ok(await Sender.Send(new GetTaSummariesQuery(semesterId), ct));

    [HttpGet("tas/{userId:guid}")]
    public async Task<ActionResult<ProctorDetailDto>> Ta(Guid userId, [FromQuery] Guid? semesterId, CancellationToken ct) =>
        Ok(await Sender.Send(new GetProctorDetailQuery(userId, semesterId), ct));

    [HttpGet("courses")]
    public async Task<ActionResult<List<CourseSummaryDto>>> Courses([FromQuery] Guid? semesterId, CancellationToken ct) =>
        Ok(await Sender.Send(new GetCourseSummariesQuery(semesterId), ct));

    [HttpGet("analytics")]
    public async Task<ActionResult<AnalyticsDto>> Analytics([FromQuery] Guid? semesterId, CancellationToken ct) =>
        Ok(await Sender.Send(new GetAnalyticsQuery(semesterId), ct));

    [HttpGet("export/workload")]
    public async Task<FileContentResult> ExportWorkload([FromQuery] Guid? semesterId, CancellationToken ct) =>
        Download(await Sender.Send(new ExportWorkloadQuery(semesterId), ct));
}
