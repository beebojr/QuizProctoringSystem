using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QPS.Application.Features.Quizzes.Queries;
using QPS.Application.Features.Schedules.Commands;
using QPS.Application.Features.Schedules.DTOs;
using QPS.Application.Features.Schedules.Queries;
using QPS.Application.Common.Interfaces;

namespace QPS.API.Controllers;

[Authorize]
public class SchedulesController : ApiControllerBase
{
    /// <summary>Admins see any TA; TAs always get their own schedule.</summary>
    [HttpGet]
    public async Task<ActionResult<List<ScheduleSlotDto>>> Get([FromQuery] Guid? userId, [FromQuery] Guid? semesterId, CancellationToken ct) =>
        Ok(await Sender.Send(new GetSchedulesQuery(userId, semesterId), ct));

    [HttpGet("overview")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<List<TaScheduleSummaryDto>>> Overview([FromQuery] Guid? semesterId, CancellationToken ct) =>
        Ok(await Sender.Send(new GetScheduleOverviewQuery(semesterId), ct));

    [HttpPost]
    public async Task<ActionResult> Create(CreateScheduleSlotCommand command, CancellationToken ct) =>
        FromResult(await Sender.Send(command, ct));

    [HttpPut("{id:guid}")]
    public async Task<ActionResult> Update(Guid id, UpdateScheduleSlotCommand command, CancellationToken ct) =>
        FromResult(await Sender.Send(command with { Id = id }, ct));

    [HttpDelete("{id:guid}")]
    public async Task<ActionResult> Delete(Guid id, CancellationToken ct) =>
        FromResult(await Sender.Send(new DeleteScheduleSlotCommand(id), ct));

    [HttpPost("import")]
    [Authorize(Roles = "Admin")]
    [RequestSizeLimit(16 * 1024 * 1024)]
    public async Task<ActionResult> Import(IFormFile file, [FromForm] Guid? semesterId, [FromForm] bool replaceExisting, CancellationToken ct)
    {
        await using var stream = await ReadExcelAsync(file, ct)
                                 ?? throw new BadHttpRequestException("Choose a file to upload.");
        return FromResult(await Sender.Send(new ImportSchedulesCommand(stream, semesterId, replaceExisting), ct));
    }

    [HttpGet("import/template")]
    [Authorize(Roles = "Admin")]
    public FileContentResult Template([FromServices] IExcelExportService excel) =>
        Download(new FileDto(excel.ScheduleTemplate(), "TA-schedule-template.xlsx", ExportQuizzesHandler.Xlsx));
}
