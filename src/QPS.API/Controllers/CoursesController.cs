using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QPS.Application.Common.Models;
using QPS.Application.Features.Courses.Commands;
using QPS.Application.Features.Courses.DTOs;
using QPS.Application.Features.Courses.Queries;

namespace QPS.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class CoursesController : ControllerBase
{
    private readonly ISender _sender;

    public CoursesController(ISender sender) => _sender = sender;

    [HttpGet]
    public async Task<ActionResult<List<CourseDto>>> GetCourses(
        [FromQuery] string? department = null,
        CancellationToken cancellationToken = default)
    {
        var result = await _sender.Send(new GetCoursesQuery(department), cancellationToken);
        return Ok(result);
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<Result<CourseDto>>> CreateCourse(
        CreateCourseCommand command, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(command, cancellationToken);
        if (!result.IsSuccess)
            return BadRequest(result);
        return CreatedAtAction(nameof(GetCourses), result);
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<Result>> DeleteCourse(
        Guid id, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new DeleteCourseCommand(id), cancellationToken);
        if (!result.IsSuccess)
            return NotFound(result);
        return Ok(result);
    }
}