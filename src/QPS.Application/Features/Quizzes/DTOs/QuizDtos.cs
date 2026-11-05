using QPS.Domain.Enums;

namespace QPS.Application.Features.Quizzes.DTOs;

public record ProctorChipDto(
    Guid AssignmentId,
    Guid UserId,
    string Name,
    string Email,
    bool IsCourseTa,
    AssignmentSource Source,
    string? OverrideReason,
    List<string> Warnings);

public record QuizLocationDto(
    Guid Id,
    string RoomName,
    string? Group,
    int ProctorsNeeded,
    bool IsReserve,
    int SortOrder,
    int Assigned,
    List<ProctorChipDto> Proctors);

public record QuizDto(
    Guid Id,
    Guid CourseId,
    string CourseName,
    Guid SemesterId,
    ExamType ExamType,
    int WeekNumber,
    DateOnly QuizDate,
    TimeOnly StartTime,
    TimeOnly EndTime,
    int? SlotNumber,
    string SlotLabel,
    QuizStatus Status,
    bool AutoAssign,
    bool HasReserve,
    string? Notes,
    QuizSource Source,
    int ProctorsNeeded,
    int ProctorsAssigned,
    bool IsFullyStaffed,
    int WarningCount,
    List<QuizLocationDto> Locations);

/// <summary>A room in the quiz form. Id is set when editing an existing location.</summary>
public record LocationInput(Guid? Id, string RoomName, string? Group, int ProctorsNeeded, bool IsReserve = false);

public record ShortfallDto(
    Guid QuizId,
    Guid LocationId,
    string CourseName,
    DateOnly Date,
    TimeOnly StartTime,
    TimeOnly EndTime,
    string RoomName,
    int Missing,
    List<string> Reasons);

public record AutoAssignSummaryDto(int Assigned, int Missing, List<ShortfallDto> Shortfalls);

public record QuizSaveResultDto(QuizDto Quiz, List<string> Warnings, AutoAssignSummaryDto? AutoAssign);
