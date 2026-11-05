namespace QPS.Application.Common.Interfaces;

/// <summary>A row of the university quiz calendar exactly as written in the file.</summary>
public sealed record RawCalendarRow(
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

public sealed record CalendarReadResult(
    IReadOnlyList<RawCalendarRow> Rows,
    IReadOnlyList<string> Notes,
    string? SheetName);

public sealed record RawScheduleRow(
    int RowNumber,
    string? Email,
    string? Day,
    string? Slot,
    string? Course,
    string? Type,
    string? Room);

public interface IExcelParserService
{
    /// <summary>
    /// Reads the quiz calendar (WEEK, DATE, SUBJECT, SLOT, GP, LOCATION). Merged cells are
    /// expanded so every row gets its week, date, subject and slot.
    /// </summary>
    CalendarReadResult ReadQuizCalendar(Stream stream);

    /// <summary>Reads TA weekly schedules (Email, Day, Slot, Course, Type, Room).</summary>
    IReadOnlyList<RawScheduleRow> ReadSchedule(Stream stream);
}
