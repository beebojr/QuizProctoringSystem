using QPS.Application.Features.Quizzes.DTOs;
using QPS.Domain.Enums;

namespace QPS.Application.Features.Assignments.DTOs;

public record AssignmentDto(
    Guid AssignmentId,
    Guid UserId,
    string TaName,
    string TaEmail,
    Guid QuizId,
    Guid LocationId,
    string CourseName,
    ExamType ExamType,
    DateOnly Date,
    TimeOnly StartTime,
    TimeOnly EndTime,
    int WeekNumber,
    string SlotLabel,
    string RoomName,
    string? Group,
    bool IsReserve,
    bool IsCourseTa,
    AssignmentSource Source,
    QuizStatus Status,
    double Points,
    List<string> CoProctors);

public record RuleViolationDto(string Code, string Message);

public record TeachingSlotBriefDto(int SlotNumber, TimeOnly StartTime, TimeOnly EndTime, string CourseName, string Type);

public record CandidateDto(
    Guid UserId,
    string Name,
    string Email,
    bool Eligible,
    bool IsCourseTa,
    List<RuleViolationDto> Violations,
    int SessionsThisSemester,
    int SessionsThisWeek,
    int MaxSessions,
    int MaxPerWeek,
    double Points,
    double TargetWorkload,
    List<TeachingSlotBriefDto> TeachingThatDay);

public record CandidatesDto(
    Guid LocationId,
    string RoomName,
    bool IsReserve,
    Guid QuizId,
    string CourseName,
    DateOnly Date,
    TimeOnly StartTime,
    TimeOnly EndTime,
    int WeekNumber,
    int ProctorsNeeded,
    int Assigned,
    List<CandidateDto> Candidates);

public record AssignOutcomeDto(bool Assigned, Guid? AssignmentId, List<RuleViolationDto> Violations, string Message);

public record ReplaceOutcomeDto(bool Replaced, string? NewProctorName, string Message);

public record BatchItemDto(
    Guid QuizId,
    Guid LocationId,
    Guid UserId,
    string CourseName,
    DateOnly Date,
    TimeOnly StartTime,
    TimeOnly EndTime,
    int WeekNumber,
    string RoomName,
    bool IsReserve,
    string TaName,
    bool IsCourseTa);

public record BatchPreviewDto(int QuizCount, int EmptySeats, List<BatchItemDto> Items, List<ShortfallDto> Shortfalls);

public record BatchCommitItem(Guid LocationId, Guid UserId);

public record BatchCommitResultDto(int Assigned, List<string> Rejected);
