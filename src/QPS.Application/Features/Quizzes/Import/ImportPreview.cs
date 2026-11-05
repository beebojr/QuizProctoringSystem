using MediatR;
using Microsoft.EntityFrameworkCore;
using QPS.Application.Common.Interfaces;
using QPS.Application.Common.Models;
using QPS.Application.Common.Services;
using QPS.Domain.Entities;
using QPS.Domain.Enums;
using QPS.Domain.Scheduling;

namespace QPS.Application.Features.Quizzes.Import;

/// <summary>
/// Reads a quiz calendar (Excel file, or rows the browser extracted from a PDF) and shows
/// what would be created. Nothing is saved.
/// </summary>
public record ImportPreviewCommand(Guid? SemesterId, Stream? ExcelFile, List<RawRowInput>? Rows) : IRequest<Result<ImportPreviewDto>>;

public class ImportPreviewHandler : IRequestHandler<ImportPreviewCommand, Result<ImportPreviewDto>>
{
    private readonly IApplicationDbContext _db;
    private readonly IExcelParserService _excel;

    public ImportPreviewHandler(IApplicationDbContext db, IExcelParserService excel)
    {
        _db = db;
        _excel = excel;
    }

    public async Task<Result<ImportPreviewDto>> Handle(ImportPreviewCommand request, CancellationToken ct)
    {
        var issues = new List<ImportIssueDto>();
        List<RawRowInput> rows;
        string? sheet = null;

        if (request.ExcelFile is not null)
        {
            CalendarReadResult read;
            try
            {
                read = _excel.ReadQuizCalendar(request.ExcelFile);
            }
            catch (Exception ex) when (ex is InvalidDataException or InvalidOperationException or ArgumentException or FormatException)
            {
                return Result<ImportPreviewDto>.Fail($"That file couldn't be read as a quiz calendar: {ex.Message}");
            }
            sheet = read.SheetName;
            issues.AddRange(read.Notes.Select(n => new ImportIssueDto(0, ImportIssueSeverity.Info, n)));
            rows = read.Rows.Select(r => new RawRowInput(r.RowNumber, r.Week, r.Date, r.Subject, r.Slot, r.Group,
                r.Location, r.StartTime, r.EndTime, r.Proctors, r.ExamType)).ToList();
        }
        else
        {
            rows = request.Rows ?? new List<RawRowInput>();
        }

        if (rows.Count == 0)
            return Result<ImportPreviewDto>.Fail("No quiz rows were found. The file needs DATE, SUBJECT, SLOT and LOCATION columns.");

        var semester = await _db.ResolveSemesterAsync(request.SemesterId, ct);
        var settings = await _db.GetSettingsAsync(ct);
        var courses = (await _db.Courses.AsNoTracking().ToListAsync(ct))
            .GroupBy(c => c.NormalizedName).ToDictionary(g => g.Key, g => g.First());
        var rooms = (await _db.Rooms.AsNoTracking().ToListAsync(ct))
            .GroupBy(r => r.NormalizedName).ToDictionary(g => g.Key, g => g.First());

        var groups = new Dictionary<string, Draft>();
        foreach (var row in rows)
        {
            var subject = NameKey.Clean(row.Subject);
            var location = NameKey.Clean(row.Location);
            if (subject.Length == 0 && location.Length == 0) continue; // empty day in the calendar
            if (location.Length == 0)
            {
                issues.Add(new(row.RowNumber, ImportIssueSeverity.Skipped, $"\"{subject}\" has no location, so it isn't a quiz."));
                continue;
            }
            if (subject.Length == 0)
            {
                issues.Add(new(row.RowNumber, ImportIssueSeverity.Error, $"Rooms {location} have no subject."));
                continue;
            }
            if (!CalendarText.TryParseDate(row.Date, out var date))
            {
                issues.Add(new(row.RowNumber, ImportIssueSeverity.Error, $"\"{row.Date}\" isn't a date I can read (expected e.g. Tuesday, April 21, 2026)."));
                continue;
            }

            TimeOnly start, end;
            int? slotNumber;
            if (CalendarText.TryParseTime(row.StartTime, out var s) && CalendarText.TryParseTime(row.EndTime, out var e) && e > s)
            {
                start = s;
                end = e;
                slotNumber = SlotCatalog.FromTimes(s, e)?.Number;
            }
            else if (!CalendarText.TryParseSlot(row.Slot, out start, out end, out slotNumber))
            {
                issues.Add(new(row.RowNumber, ImportIssueSeverity.Error, $"\"{row.Slot}\" isn't a slot (expected 1ST–5TH or Gap)."));
                continue;
            }

            var roomNames = CalendarText.SplitRooms(location);
            if (roomNames.Count == 0)
            {
                issues.Add(new(row.RowNumber, ImportIssueSeverity.Error, $"No room codes found in \"{location}\"."));
                continue;
            }

            var courseKey = NameKey.For(subject);
            var key = $"{courseKey}|{date:yyyy-MM-dd}|{start:HH\\:mm}|{end:HH\\:mm}";
            if (!groups.TryGetValue(key, out var draft))
            {
                courses.TryGetValue(courseKey, out var course);
                draft = new Draft(key, course?.Name ?? subject, course?.Id, date, start, end, slotNumber,
                    CalendarText.ParseWeek(row.Week), ParseExamType(row.ExamType));
                groups[key] = draft;
            }
            draft.Rows.Add(row.RowNumber);

            int? explicitProctors = int.TryParse(row.Proctors, out var p) && p is >= 1 and <= 10 ? p : null;
            var group = NameKey.Clean(row.Group);
            foreach (var room in roomNames)
            {
                var roomKey = NameKey.For(room);
                var existing = draft.Locations.FirstOrDefault(l => NameKey.For(l.Room) == roomKey);
                if (existing is not null)
                {
                    if (group.Length > 0 && !(existing.Group ?? "").Split('/').Contains(group))
                        existing.Group = string.IsNullOrEmpty(existing.Group) ? group : $"{existing.Group}/{group}";
                    continue;
                }
                rooms.TryGetValue(roomKey, out var known);
                draft.Locations.Add(new DraftLocation(room, group.Length == 0 ? null : group,
                    explicitProctors ?? known?.DefaultProctors ?? settings.DefaultProctorsPerRoom, known is not null));
            }
        }

        // Quizzes that already exist (same course, date and start) only receive new rooms.
        var existingQuizzes = await _db.Quizzes.AsNoTracking()
            .Include(q => q.Locations)
            .Where(q => q.SemesterId == semester.Id)
            .Select(q => new { q.Id, q.CourseId, q.QuizDate, q.StartTime, Rooms = q.Locations.Select(l => l.RoomName).ToList() })
            .ToListAsync(ct);

        var previews = new List<ImportQuizPreviewDto>();
        foreach (var d in groups.Values.OrderBy(d => d.Date).ThenBy(d => d.Start).ThenBy(d => d.CourseName))
        {
            var warnings = new List<string>();
            var computedWeek = AcademicCalendar.WeekNumber(semester.StartDate, d.Date);
            if (d.CourseId is null) warnings.Add($"\"{d.CourseName}\" is not in the course list; it will be added.");
            if (d.Date.DayOfWeek == System.DayOfWeek.Friday) warnings.Add("This date is a Friday — every TA is off.");
            if (d.Date < semester.StartDate || d.Date > semester.EndDate)
                warnings.Add($"Outside {semester.Name} ({Format.Date(semester.StartDate)} – {Format.Date(semester.EndDate)}).");
            // Holidays can shift the official week labels by one, so only a bigger gap is suspicious.
            if (d.FileWeek.HasValue && Math.Abs(d.FileWeek.Value - computedWeek) >= 2)
                warnings.Add($"The file says week {d.FileWeek}, but by date this is week {computedWeek} of {semester.Name}. Check the semester start date in Settings.");

            var existing = d.CourseId is null ? null : existingQuizzes.FirstOrDefault(q =>
                q.CourseId == d.CourseId && q.QuizDate == d.Date && q.StartTime == d.Start);
            var existingRooms = existing?.Rooms.Select(NameKey.For).ToHashSet() ?? new HashSet<string>();
            if (existing is not null)
            {
                var newRooms = d.Locations.Count(l => !existingRooms.Contains(NameKey.For(l.Room)));
                warnings.Add(newRooms == 0 ? "Already imported — nothing new." : $"Already exists — {newRooms} new room(s) will be added.");
            }

            previews.Add(new ImportQuizPreviewDto(
                d.Key, d.CourseName, d.CourseId, d.CourseId is null, d.ExamType, d.Date, d.Start, d.End, d.SlotNumber,
                SlotCatalog.Describe(d.SlotNumber), d.FileWeek ?? computedWeek, d.FileWeek, existing?.Id,
                d.Locations.Select(l => new ImportLocationPreviewDto(l.Room, l.Group, l.Proctors,
                    existingRooms.Contains(NameKey.For(l.Room)), l.Known)).ToList(),
                warnings, d.Rows));
        }

        return Result<ImportPreviewDto>.Ok(new ImportPreviewDto(semester.Id, semester.Name, sheet, rows.Count, previews,
            issues.OrderBy(i => i.Row).ToList()));
    }

    private static ExamType ParseExamType(string? text)
    {
        var t = (text ?? "").Trim().ToLowerInvariant();
        if (t.StartsWith("mid")) return ExamType.Midterm;
        if (t.StartsWith("fin")) return ExamType.Final;
        return ExamType.Quiz;
    }

    private sealed class Draft
    {
        public Draft(string key, string courseName, Guid? courseId, DateOnly date, TimeOnly start, TimeOnly end,
            int? slotNumber, int? fileWeek, ExamType examType)
        {
            Key = key;
            CourseName = courseName;
            CourseId = courseId;
            Date = date;
            Start = start;
            End = end;
            SlotNumber = slotNumber;
            FileWeek = fileWeek;
            ExamType = examType;
        }

        public string Key { get; }
        public string CourseName { get; }
        public Guid? CourseId { get; }
        public DateOnly Date { get; }
        public TimeOnly Start { get; }
        public TimeOnly End { get; }
        public int? SlotNumber { get; }
        public int? FileWeek { get; }
        public ExamType ExamType { get; }
        public List<DraftLocation> Locations { get; } = new();
        public List<int> Rows { get; } = new();
    }

    private sealed class DraftLocation
    {
        public DraftLocation(string room, string? group, int proctors, bool known)
        {
            Room = room;
            Group = group;
            Proctors = proctors;
            Known = known;
        }

        public string Room { get; }
        public string? Group { get; set; }
        public int Proctors { get; }
        public bool Known { get; }
    }
}
