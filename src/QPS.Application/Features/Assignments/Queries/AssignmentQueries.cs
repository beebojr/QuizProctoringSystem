using MediatR;
using Microsoft.EntityFrameworkCore;
using QPS.Application.Common.Exceptions;
using QPS.Application.Common.Interfaces;
using QPS.Application.Common.Services;
using QPS.Application.Features.Assignments.DTOs;
using QPS.Domain.Entities;
using QPS.Domain.Rules;
using QPS.Domain.Scheduling;

namespace QPS.Application.Features.Assignments.Queries;

public record GetAssignmentsQuery(Guid? SemesterId = null, Guid? UserId = null, Guid? QuizId = null) : IRequest<List<AssignmentDto>>;

/// <summary>The signed-in TA's own proctoring sessions.</summary>
public record GetMyAssignmentsQuery(Guid? SemesterId = null) : IRequest<List<AssignmentDto>>;

/// <summary>Every TA with whether they can take a seat in this location, and why not.</summary>
public record GetCandidatesQuery(Guid LocationId) : IRequest<CandidatesDto>;

public class AssignmentQueryHandlers :
    IRequestHandler<GetAssignmentsQuery, List<AssignmentDto>>,
    IRequestHandler<GetMyAssignmentsQuery, List<AssignmentDto>>,
    IRequestHandler<GetCandidatesQuery, CandidatesDto>
{
    private readonly IApplicationDbContext _db;
    private readonly IClock _clock;
    private readonly ICurrentUserService _currentUser;
    private readonly AssignmentWorkspace _workspace;

    public AssignmentQueryHandlers(IApplicationDbContext db, IClock clock, ICurrentUserService currentUser, AssignmentWorkspace workspace)
    {
        _db = db;
        _clock = clock;
        _currentUser = currentUser;
        _workspace = workspace;
    }

    public async Task<List<AssignmentDto>> Handle(GetAssignmentsQuery request, CancellationToken ct)
    {
        var semester = await _db.ResolveSemesterAsync(request.SemesterId, ct);
        return await LoadAsync(semester.Id, request.UserId, request.QuizId, ct);
    }

    public async Task<List<AssignmentDto>> Handle(GetMyAssignmentsQuery request, CancellationToken ct)
    {
        var semester = await _db.ResolveSemesterAsync(request.SemesterId, ct);
        return await LoadAsync(semester.Id, _currentUser.RequireUserId(), null, ct);
    }

    private async Task<List<AssignmentDto>> LoadAsync(Guid semesterId, Guid? userId, Guid? quizId, CancellationToken ct)
    {
        var query = _db.ProctorAssignments.AsNoTracking()
            .Include(a => a.User)
            .Include(a => a.QuizLocation).ThenInclude(l => l.Quiz).ThenInclude(q => q.Course)
            .Where(a => a.QuizLocation.Quiz.SemesterId == semesterId);
        if (userId.HasValue) query = query.Where(a => a.UserId == userId.Value);
        if (quizId.HasValue) query = query.Where(a => a.QuizLocation.QuizId == quizId.Value);
        var assignments = await query.ToListAsync(ct);

        var quizIds = assignments.Select(a => a.QuizLocation.QuizId).Distinct().ToList();
        var coProctors = (await _db.ProctorAssignments.AsNoTracking()
                .Where(a => quizIds.Contains(a.QuizLocation.QuizId))
                .Select(a => new { a.QuizLocationId, a.UserId, Name = a.User.FirstName + " " + a.User.LastName })
                .ToListAsync(ct))
            .ToLookup(x => x.QuizLocationId);

        var now = _clock.Now;
        return assignments
            .OrderBy(a => a.QuizLocation.Quiz.QuizDate).ThenBy(a => a.QuizLocation.Quiz.StartTime)
            .Select(a => ToDto(a, now, coProctors[a.QuizLocationId].Where(c => c.UserId != a.UserId).Select(c => c.Name).ToList()))
            .ToList();
    }

    public static AssignmentDto ToDto(ProctorAssignment a, DateTime now, List<string> coProctors)
    {
        var q = a.QuizLocation.Quiz;
        return new AssignmentDto(
            a.Id, a.UserId, a.User.FullName, a.User.Email, q.Id, a.QuizLocationId, q.Course.Name, q.ExamType,
            q.QuizDate, q.StartTime, q.EndTime, q.WeekNumber, SlotCatalog.Describe(q.SlotNumber),
            a.QuizLocation.RoomName, a.QuizLocation.Group, a.QuizLocation.IsReserve, a.IsCourseTa, a.Source,
            QuizTiming.StatusAt(q.QuizDate, q.StartTime, q.EndTime, now), a.Points, coProctors);
    }

    public async Task<CandidatesDto> Handle(GetCandidatesQuery request, CancellationToken ct)
    {
        var location = await _db.QuizLocations.AsNoTracking()
            .Include(l => l.Assignments)
            .Include(l => l.Quiz).ThenInclude(q => q.Course)
            .FirstOrDefaultAsync(l => l.Id == request.LocationId, ct)
            ?? throw new NotFoundException("Location", request.LocationId);
        var quiz = location.Quiz;
        var context = await _workspace.LoadAsync(quiz.SemesterId, ct);
        var session = context.SessionFor(quiz);
        var day = AcademicCalendar.ToDomainDay(quiz.QuizDate);
        var profileIds = context.Profiles.Keys.ToList();
        var users = await _db.Users.AsNoTracking().Where(u => profileIds.Contains(u.Id)).ToDictionaryAsync(u => u.Id, ct);

        var ranked = AssignmentEngine.Rank(context.Profiles.Values, session, context.Ledger)
            .Select((p, index) => (p, index)).ToDictionary(x => x.p.Id, x => x.index);

        // Deactivated accounts can never proctor, so they are not offered at all.
        var candidates = context.Profiles.Values.Where(p => p.IsActive).Select(p =>
        {
            var violations = context.Evaluate(p.Id, session);
            var semesterBookings = context.BookingsOf(p.Id).Where(b => b.SemesterId == quiz.SemesterId).ToList();
            return new CandidateDto(
                p.Id, p.Name, users.TryGetValue(p.Id, out var u) ? u.Email : string.Empty,
                violations.Count == 0, p.CourseIds.Contains(quiz.CourseId),
                violations.Select(v => new RuleViolationDto(v.Code.ToString(), v.Message)).ToList(),
                semesterBookings.Count, semesterBookings.Count(b => b.WeekKey == session.WeekKey),
                p.MaxSessionsPerSemester, context.Rules.MaxSessionsPerWeek,
                semesterBookings.Sum(b => b.Points), p.TargetWorkload,
                p.Teaching.Where(t => t.Day == day).OrderBy(t => t.Start)
                    .Select(t => new TeachingSlotBriefDto(t.SlotNumber, t.Start, t.End, t.CourseName, t.Type.ToString())).ToList());
        })
        .OrderByDescending(c => c.Eligible)
        .ThenByDescending(c => c.Eligible && c.IsCourseTa)
        .ThenBy(c => c.Violations.Any(v => v.Code == nameof(RuleCode.AlreadyInQuiz)))
        .ThenBy(c => c.Violations.Count)
        .ThenBy(c => ranked.GetValueOrDefault(c.UserId))
        .ToList();

        return new CandidatesDto(location.Id, location.RoomName, location.IsReserve, quiz.Id, quiz.Course.Name, quiz.QuizDate,
            quiz.StartTime, quiz.EndTime, quiz.WeekNumber, location.ProctorsNeeded, location.Assignments.Count, candidates);
    }
}
