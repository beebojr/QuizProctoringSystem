namespace QPS.Application.Features.Semesters.DTOs;

public record SemesterDto(
    Guid Id,
    string Name,
    DateOnly StartDate,
    DateOnly EndDate,
    bool IsActive,
    int CurrentWeek,
    int TotalWeeks);
