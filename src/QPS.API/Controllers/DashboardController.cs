using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QPS.Application.Features.Dashboard.DTOs;
using QPS.Application.Features.Dashboard.Queries;

namespace QPS.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class DashboardController : ControllerBase
{
    private readonly ISender _sender;

    public DashboardController(ISender sender) => _sender = sender;

    [HttpGet("workload/{userId}")]
    public async Task<ActionResult<WorkloadSummaryDto>> GetWorkload(
        Guid userId, [FromQuery] Guid semesterId, CancellationToken ct)
    {
        var result = await _sender.Send(new GetWorkloadSummaryQuery(userId, semesterId), ct);
        return Ok(result);
    }

    [HttpGet]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<SystemDashboardDto>> GetSystemDashboard(
        CancellationToken ct)
    {
        var result = await _sender.Send(new GetSystemDashboardQuery(), ct);
        return Ok(result);
    }

    [HttpGet("report")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ReportDto>> GetReport(
        [FromQuery] DateOnly fromDate,
        [FromQuery] DateOnly toDate,
        [FromQuery] Guid? semesterId = null,
        CancellationToken ct = default)
    {
        var result = await _sender.Send(new GetReportQuery(fromDate, toDate, semesterId), ct);
        return Ok(result);
    }
}