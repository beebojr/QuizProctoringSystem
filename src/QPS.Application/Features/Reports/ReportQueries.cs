using MediatR;
using Microsoft.EntityFrameworkCore;
using QPS.Application.Common.Exceptions;
using QPS.Application.Common.Interfaces;
using QPS.Application.Common.Services;
using QPS.Application.Features.Assignments.Queries;
using QPS.Application.Features.Excuses;
using QPS.Application.Features.Quizzes.Queries;
using QPS.Domain.Enums;
using QPS.Domain.Scheduling;

namespace QPS.Application.Features.Reports;

public record GetTaSummariesQuery(Guid? SemesterId = null) : IRequest<List<TaSummaryDto>>;
public record GetProctorDetailQuery(Guid UserId, Guid? SemesterId = null) : IRequest<ProctorDetailDto>;
public record GetCourseSummariesQuery(Guid? SemesterId = null) : IRequest<List<CourseSummaryDto>>;
public record GetAnalyticsQuery(Guid? SemesterId = null) : IRequest<AnalyticsDto>;
public record GetAdminDashboardQuery(Guid? SemesterId = null) : IRequest<AdminDashboardDto>;
public record GetTaDashboardQuery : IRequest<TaDashboardDto>;
public record ExportWorkloadQuery(Guid? SemesterId = null) : IRequest<FileDto>;

public class ReportQueryHandlers :
    IRequestHandler<GetTaSummariesQuery, List<TaSummaryDto>>,
    IRequestHandler<GetProctorDetailQuery, ProctorDetailDto>,
    IRequestHandler<GetCourseSummariesQuery, List<CourseSummaryDto>>,
    IRequestHandler<GetAnalyticsQuery, AnalyticsDto>,
    IRequestHandler<GetAdminDashboardQuery, AdminDashboardDto>,
    IRequestHandler<GetTaDashboardQuery, TaDashboardDto>,
    IRequestHandler<ExportWorkloadQuery, FileDto>
{
    private readonly IApplicationDbContext _db;
    private readonly IClock _clock;
    private readonly ICurrentUserService _currentUser;
    private readonly AssignmentWorkspace _workspace;
    private readonly IExcelExportService _excel;

    public ReportQueryHandlers(IApplicationDbContext db, IClock clock, ICurrentUserService currentUser,
        AssignmentWorkspace workspace, IExcelExportService excel)
    {
        _db = db;
        _clock = clock;
        _currentUser = currentUser;
        _workspace = workspace;
        _excel = excel;
    }

    private Task<SemesterSnapshot> Snapshot(Guid? semesterId, CancellationToken ct) =>
        SemesterSnapshot.LoadAsync(_db, semesterId, _clock.Now, ct);

    public async Task<List<TaSummaryDto>> Handle(GetTaSummariesQuery request, CancellationToken ct) =>
        ReportBuilder.TaSummaries(await Snapshot(request.SemesterId, ct));

    public async Task<ProctorDetailDto> Handle(GetProctorDetailQuery request, CancellationToken ct)
    {
        var s = await Snapshot(request.SemesterId, ct);
        var summary = ReportBuilder.TaSummaries(s).FirstOrDefault(t => t.UserId == request.UserId)
                      ?? throw new NotFoundException("TA", request.UserId);
        var now = _clock.Now;
        var mine = s.Assignments.Where(x => x.Assignment.UserId == request.UserId).ToList();
        var history = mine
            .OrderByDescending(x => x.Quiz.QuizDate).ThenByDescending(x => x.Quiz.StartTime)
            .Select(x =>
            {
                x.Location.Quiz = x.Quiz;
                x.Assignment.QuizLocation = x.Location;
                var co = x.Location.Assignments.Where(a => a.UserId != request.UserId).Select(a => a.User.FullName).ToList();
                return AssignmentQueryHandlers.ToDto(x.Assignment, now, co);
            })
            .ToList();
        var excuses = await _db.Excuses.AsNoTracking().Include(e => e.User)
            .Where(e => e.UserId == request.UserId).OrderByDescending(e => e.StartDate).ToListAsync(ct);
        return new ProctorDetailDto(summary, history, excuses.Select(e => ExcuseHandlers.ToDto(e, s.Today)).ToList());
    }

    public async Task<List<CourseSummaryDto>> Handle(GetCourseSummariesQuery request, CancellationToken ct)
    {
        var s = await Snapshot(request.SemesterId, ct);
        var courses = await _db.Courses.AsNoTracking().ToListAsync(ct);
        return ReportBuilder.CourseSummaries(s, courses);
    }

    public async Task<AnalyticsDto> Handle(GetAnalyticsQuery request, CancellationToken ct) =>
        ReportBuilder.Analytics(await Snapshot(request.SemesterId, ct));

    public async Task<AdminDashboardDto> Handle(GetAdminDashboardQuery request, CancellationToken ct)
    {
        var s = await Snapshot(request.SemesterId, ct);
        var context = await _workspace.LoadAsync(s.Semester.Id, ct);
        var upcoming = s.Quizzes.Where(q => s.StatusOf(q) == QuizStatus.Upcoming)
            .OrderBy(q => q.QuizDate).ThenBy(q => q.StartTime).ToList();
        var missing = upcoming.Where(q => q.Locations.Any(l => l.Assignments.Count < l.ProctorsNeeded)).ToList();
        var flagged = upcoming.Sum(q =>
        {
            var session = context.SessionFor(q);
            return q.Locations.SelectMany(l => l.Assignments).Count(a => context.Revalidate(a.UserId, session).Count > 0);
        });

        var summaries = ReportBuilder.TaSummaries(s);
        var weekStart = AcademicCalendar.WeekStart(s.Semester.StartDate, s.CurrentWeek);
        var days = Enumerable.Range(0, 7).Select(i => weekStart.AddDays(i))
            .Where(d => d.DayOfWeek != System.DayOfWeek.Friday)
            .Select(d =>
            {
                var qs = s.Quizzes.Where(q => q.QuizDate == d).ToList();
                return new DayLoadDto(d, qs.Count, qs.Sum(q => q.Locations.Sum(l => l.ProctorsNeeded)),
                    qs.Sum(q => q.Locations.Sum(l => Math.Min(l.Assignments.Count, l.ProctorsNeeded))));
            })
            .ToList();

        return new AdminDashboardDto(
            s.Settings.DepartmentName, s.Semester.Id, s.Semester.Name, s.CurrentWeek,
            AcademicCalendar.WeekNumber(s.Semester.StartDate, s.Semester.EndDate),
            s.Tas.Count(t => t.IsActive),
            upcoming.Count,
            missing.Count,
            upcoming.Sum(q => q.Locations.Sum(l => Math.Max(0, l.ProctorsNeeded - l.Assignments.Count))),
            flagged,
            s.Excuses.Count(e => e.EndDate >= s.Today && s.Tas.Any(t => t.Id == e.UserId)),
            summaries.Count(t => t.IsActive && t.SlotCount == 0),
            upcoming.Take(8).Select(q => ReportBuilder.Brief(s, q)).ToList(),
            summaries.Where(t => t.Assignments > 0)
                .OrderByDescending(t => t.Assignments).ThenByDescending(t => t.Points).ThenBy(t => t.Name)
                .Take(5).Select(t => new TaRankDto(t.UserId, t.Name, t.Assignments, t.Points)).ToList(),
            days);
    }

    public async Task<TaDashboardDto> Handle(GetTaDashboardQuery request, CancellationToken ct)
    {
        var userId = _currentUser.RequireUserId();
        var s = await Snapshot(null, ct);
        var me = s.Tas.FirstOrDefault(t => t.Id == userId)
                 ?? throw new ForbiddenAccessException("The TA dashboard is only for TAs.");
        var summary = ReportBuilder.TaSummaries(s).First(t => t.UserId == userId);
        var now = _clock.Now;
        var upcoming = s.Assignments
            .Where(x => x.Assignment.UserId == userId && s.StatusOf(x.Quiz) != QuizStatus.Finished)
            .OrderBy(x => x.Quiz.QuizDate).ThenBy(x => x.Quiz.StartTime)
            .Select(x =>
            {
                x.Location.Quiz = x.Quiz;
                x.Assignment.QuizLocation = x.Location;
                var co = x.Location.Assignments.Where(a => a.UserId != userId).Select(a => a.User.FullName).ToList();
                return AssignmentQueryHandlers.ToDto(x.Assignment, now, co);
            })
            .ToList();
        var excuses = s.Excuses.Where(e => e.UserId == userId && e.EndDate >= s.Today)
            .OrderBy(e => e.StartDate).Select(e => { e.User = me; return ExcuseHandlers.ToDto(e, s.Today); }).ToList();

        return new TaDashboardDto(
            me.FullName, s.Semester.Name, s.CurrentWeek, summary.Points, summary.TargetWorkload, summary.MaxSessions,
            summary.Assignments, summary.ThisWeek, s.Settings.MaxSessionsPerWeek, upcoming, excuses,
            summary.DaysOff, me.CanEditSchedule, summary.SlotCount);
    }

    public async Task<FileDto> Handle(ExportWorkloadQuery request, CancellationToken ct)
    {
        var s = await Snapshot(request.SemesterId, ct);
        var rows = ReportBuilder.TaSummaries(s)
            .OrderByDescending(t => t.Assignments).ThenBy(t => t.Name)
            .Select(t => (IReadOnlyList<object?>)new object?[]
            {
                t.Name, t.Email, t.IsActive ? "Active" : "Inactive", t.Assignments, t.Completed, t.Upcoming, t.Points,
                t.TargetWorkload, t.MaxSessions, t.TargetReached ? "Yes" : "No", t.SlotCount, string.Join(", ", t.DaysOff),
                t.ActiveExcuses, string.Join(", ", t.CoursesTaught)
            }).ToList();
        var bytes = _excel.BuildWorkbook(new[]
        {
            new Common.Interfaces.ExportSheet("Workload", new[]
            {
                "TA", "Email", "Status", "Assignments", "Completed", "Upcoming", "Workload points", "Target", "Semester limit",
                "Target reached", "Teaching slots", "Extra days off", "Active excuses", "Courses taught"
            }, rows)
        });
        return new FileDto(bytes, $"Workload-{ExportQuizzesHandler.Safe(s.Semester.Name)}-{_clock.Today:yyyy-MM-dd}.xlsx", ExportQuizzesHandler.Xlsx);
    }
}
