using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QPS.Application.Features.Courses.Commands;
using QPS.Application.Features.Courses.DTOs;
using QPS.Application.Features.Courses.Queries;

namespace QPS.API.Controllers;

[Authorize]
public class CoursesController : ApiControllerBase
{
    [HttpGet]
    public async Task<ActionResult<List<CourseDto>>> Get([FromQuery] string? department, [FromQuery] string? search, CancellationToken ct) =>
        Ok(await Sender.Send(new GetCoursesQuery(department, search), ct));

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult> Create(CreateCourseCommand command, CancellationToken ct) =>
        FromResult(await Sender.Send(command, ct));

    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult> Update(Guid id, UpdateCourseCommand command, CancellationToken ct) =>
        FromResult(await Sender.Send(command with { Id = id }, ct));

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult> Delete(Guid id, CancellationToken ct) =>
        FromResult(await Sender.Send(new DeleteCourseCommand(id), ct));
}
