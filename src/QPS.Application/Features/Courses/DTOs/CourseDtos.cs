using MediatR;
using QPS.Application.Common.Models;

namespace QPS.Application.Features.Courses.DTOs;

public record CourseDto(
    Guid Id,
    string Name,
    string Department,
    DateTime CreatedAt);

public record CreateCourseCommand(
    string Name,
    string Department) : IRequest<Result<CourseDto>>;