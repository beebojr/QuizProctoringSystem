using QPS.Domain.Enums;
using DayOfWeek = QPS.Domain.Enums.DayOfWeek;

namespace QPS.Application.Features.Schedules.DTOs;

public record ScheduleSlotDto(
    Guid Id,
    Guid UserId,
    string TaName,
    DayOfWeek Day,
    int SlotNumber,
    string SlotLabel,
    Guid? CourseId,
    string CourseName,
    SlotType SlotType,
    string Room,
    TimeOnly StartTime,
    TimeOnly EndTime);

public record ScheduleChangeDto(ScheduleSlotDto? Slot, List<string> Warnings);

public record TaScheduleSummaryDto(
    Guid UserId,
    string Name,
    string Email,
    List<string> DaysOff,
    int SlotCount,
    bool CanEditSchedule,
    bool IsActive);

public record ScheduleImportErrorDto(int Row, string Message);

public record ScheduleImportResultDto(int Created, int TasReplaced, List<ScheduleImportErrorDto> Errors);
