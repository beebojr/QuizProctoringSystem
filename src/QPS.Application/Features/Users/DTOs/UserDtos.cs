using QPS.Domain.Enums;
using QPS.Domain.Scheduling;
using QPS.Application.Common.Services;
using QPS.Domain.Entities;

namespace QPS.Application.Features.Users.DTOs;

public record UserDto(
    Guid Id,
    string Email,
    string FirstName,
    string LastName,
    string FullName,
    UserRole Role,
    string Department,
    List<string> DaysOff,
    int? MaxSessionsOverride,
    double? TargetWorkloadOverride,
    int EffectiveMaxSessions,
    double EffectiveTargetWorkload,
    bool CanEditSchedule,
    bool IsActive,
    DateTime? LastLoginAt)
{
    public static UserDto From(User u, SystemSettings s) => new(
        u.Id, u.Email, u.FirstName, u.LastName, u.FullName, u.Role, u.Department,
        Format.Days(u.DaysOff),
        u.MaxSessionsOverride, u.TargetWorkloadOverride,
        u.MaxSessionsOverride ?? s.DefaultMaxSessionsPerSemester,
        u.TargetWorkloadOverride ?? s.DefaultTargetWorkload,
        u.CanEditSchedule, u.IsActive, u.LastLoginAt);
}

public static class DaysOffInput
{
    /// <summary>Friday is always off, so it is never stored as an "extra" day.</summary>
    public static WeekDays ToFlags(IEnumerable<QPS.Domain.Enums.DayOfWeek>? days) =>
        AcademicCalendar.ToFlags((days ?? Enumerable.Empty<QPS.Domain.Enums.DayOfWeek>())
            .Where(d => d != QPS.Domain.Enums.DayOfWeek.Friday).Distinct());
}
