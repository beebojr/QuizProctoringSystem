using MediatR;
using Microsoft.EntityFrameworkCore;
using QPS.Application.Common.Interfaces;
using QPS.Application.Common.Services;
using QPS.Domain.Scheduling;

namespace QPS.Application.Features.Quizzes.Queries;

public record FileDto(byte[] Content, string FileName, string ContentType);

public record ExportQuizzesQuery(Guid? SemesterId = null) : IRequest<FileDto>;

public record GetImportTemplateQuery : IRequest<FileDto>;

public class ExportQuizzesHandler :
    IRequestHandler<ExportQuizzesQuery, FileDto>,
    IRequestHandler<GetImportTemplateQuery, FileDto>
{
    public const string Xlsx = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

    private readonly IApplicationDbContext _db;
    private readonly IExcelExportService _excel;
    private readonly IClock _clock;

    public ExportQuizzesHandler(IApplicationDbContext db, IExcelExportService excel, IClock clock)
    {
        _db = db;
        _excel = excel;
        _clock = clock;
    }

    public async Task<FileDto> Handle(ExportQuizzesQuery request, CancellationToken ct)
    {
        var semester = await _db.ResolveSemesterAsync(request.SemesterId, ct);
        var quizzes = await _db.Quizzes.AsNoTracking()
            .Include(q => q.Course)
            .Include(q => q.Locations).ThenInclude(l => l.Assignments).ThenInclude(a => a.User)
            .Where(q => q.SemesterId == semester.Id)
            .ToListAsync(ct);
        var now = _clock.Now;
        var ordered = quizzes.OrderBy(q => q.QuizDate).ThenBy(q => q.StartTime).ThenBy(q => q.Course.Name).ToList();

        var summary = ordered.Select(q =>
        {
            var needed = q.Locations.Sum(l => l.ProctorsNeeded);
            var assigned = q.Locations.Sum(l => Math.Min(l.Assignments.Count, l.ProctorsNeeded));
            return (IReadOnlyList<object?>)new object?[]
            {
                q.WeekNumber, q.QuizDate.ToDateTime(TimeOnly.MinValue), q.QuizDate.DayOfWeek.ToString(), q.Course.Name,
                q.ExamType.ToString(), SlotCatalog.Describe(q.SlotNumber), Format.TimeRange(q.StartTime, q.EndTime),
                string.Join(" - ", q.Locations.Where(l => !l.IsReserve).OrderBy(l => l.SortOrder).Select(l => l.RoomName)),
                needed, assigned, assigned >= needed ? "Staffed" : $"Missing {needed - assigned}",
                QuizTiming.StatusAt(q.QuizDate, q.StartTime, q.EndTime, now).ToString()
            };
        }).ToList();

        var detail = ordered.SelectMany(q => q.Locations
            .OrderBy(l => l.IsReserve).ThenBy(l => l.SortOrder)
            .SelectMany(l => l.Assignments.Count == 0
                ? new[] { Row(q, l, null) }
                : l.Assignments.OrderByDescending(a => a.IsCourseTa).Select(a => Row(q, l, a)).ToArray()))
            .ToList();

        var bytes = _excel.BuildWorkbook(new[]
        {
            new ExportSheet("Quizzes", new[] { "Week", "Date", "Day", "Course", "Type", "Slot", "Time", "Rooms", "Proctors needed", "Proctors assigned", "Staffing", "Status" }, summary),
            new ExportSheet("Proctors", new[] { "Week", "Date", "Course", "Time", "Room", "Group", "Reserve", "Proctor", "Email", "Course TA" }, detail)
        });
        return new FileDto(bytes, $"Proctoring-{Safe(semester.Name)}-{_clock.Today:yyyy-MM-dd}.xlsx", Xlsx);
    }

    public Task<FileDto> Handle(GetImportTemplateQuery request, CancellationToken ct) =>
        Task.FromResult(new FileDto(_excel.QuizCalendarTemplate(), "Quiz-calendar-template.xlsx", Xlsx));

    private static IReadOnlyList<object?> Row(Domain.Entities.Quiz q, Domain.Entities.QuizLocation l, Domain.Entities.ProctorAssignment? a) =>
        new object?[]
        {
            q.WeekNumber, q.QuizDate.ToDateTime(TimeOnly.MinValue), q.Course.Name, Format.TimeRange(q.StartTime, q.EndTime),
            l.RoomName, l.Group, l.IsReserve ? "Yes" : "", a?.User.FullName ?? "(empty seat)", a?.User.Email, a is { IsCourseTa: true } ? "Yes" : ""
        };

    public static string Safe(string name) => string.Concat(name.Where(c => char.IsLetterOrDigit(c) || c is '-' or '_')).Trim('-');
}
