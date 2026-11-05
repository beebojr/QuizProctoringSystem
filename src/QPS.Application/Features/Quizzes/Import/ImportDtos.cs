using QPS.Domain.Enums;

namespace QPS.Application.Features.Quizzes.Import;

/// <summary>A raw row sent by the browser (PDF calendars are read client-side).</summary>
public record RawRowInput(
    int RowNumber,
    string? Week,
    string? Date,
    string? Subject,
    string? Slot,
    string? Group,
    string? Location,
    string? StartTime = null,
    string? EndTime = null,
    string? Proctors = null,
    string? ExamType = null);

public enum ImportIssueSeverity
{
    Error,
    Skipped,
    Info
}

public record ImportIssueDto(int Row, ImportIssueSeverity Severity, string Message);

public record ImportLocationPreviewDto(string RoomName, string? Group, int ProctorsNeeded, bool AlreadyInQuiz, bool RoomKnown);

public record ImportQuizPreviewDto(
    string Key,
    string CourseName,
    Guid? CourseId,
    bool IsNewCourse,
    ExamType ExamType,
    DateOnly Date,
    TimeOnly StartTime,
    TimeOnly EndTime,
    int? SlotNumber,
    string SlotLabel,
    int WeekNumber,
    int? FileWeek,
    Guid? ExistingQuizId,
    List<ImportLocationPreviewDto> Locations,
    List<string> Warnings,
    List<int> SourceRows);

public record ImportPreviewDto(
    Guid SemesterId,
    string SemesterName,
    string? SheetName,
    int RowsRead,
    List<ImportQuizPreviewDto> Quizzes,
    List<ImportIssueDto> Issues);

public record ImportLocationInput(string RoomName, string? Group, int ProctorsNeeded);

public record ImportQuizInput(
    string CourseName,
    ExamType ExamType,
    DateOnly Date,
    TimeOnly StartTime,
    TimeOnly EndTime,
    List<ImportLocationInput> Locations,
    int? WeekNumber = null);

public record ImportCommitResultDto(
    int QuizzesCreated,
    int QuizzesUpdated,
    int RoomsAdded,
    List<string> CoursesCreated,
    List<string> Errors,
    QPS.Application.Features.Quizzes.DTOs.AutoAssignSummaryDto? AutoAssign);
