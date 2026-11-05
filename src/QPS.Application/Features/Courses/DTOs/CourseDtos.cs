namespace QPS.Application.Features.Courses.DTOs;

public record CourseDto(
    Guid Id,
    string Name,
    string? Code,
    string Department,
    int QuizCount,
    int TaCount);
