using Microsoft.EntityFrameworkCore;
using QPS.Application.Common.Interfaces;
using QPS.Domain.Entities;
using QPS.Domain.Enums;
using QPS.Domain.Rules;
using QPS.Domain.Scheduling;

namespace QPS.Application.Common.Services;

/// <summary>Everything the rules need for one semester, loaded once per request.</summary>
public sealed class AssignmentContext
{
    public required Semester Semester { get; init; }
    public required SystemSettings Settings { get; init; }
    public required RulesConfig Rules { get; init; }

    /// <summary>All TAs that are not deleted (inactive ones included so they can be explained).</summary>
    public required Dictionary<Guid, ProctorProfile> Profiles { get; init; }

    /// <summary>Sessions each TA is booked for in this semester.</summary>
    public required Dictionary<Guid, List<BookedSession>> Ledger { get; init; }

    public IReadOnlyList<ProctorProfile> ActivePool => Profiles.Values.Where(p => p.IsActive).ToList();

    public int WeekOf(DateOnly date) => AcademicCalendar.WeekNumber(Semester.StartDate, date);

    public QuizSession SessionFor(Quiz quiz) =>
        new(quiz.Id, quiz.SemesterId, quiz.CourseId, quiz.QuizDate, quiz.StartTime, quiz.EndTime, WeekOf(quiz.QuizDate));

    public List<BookedSession> BookingsOf(Guid userId) =>
        Ledger.TryGetValue(userId, out var list) ? list : new List<BookedSession>();

    /// <summary>Rules for putting this TA in this quiz.</summary>
    public IReadOnlyList<RuleViolation> Evaluate(Guid userId, QuizSession session)
    {
        if (!Profiles.TryGetValue(userId, out var profile))
            return new[] { new RuleViolation(RuleCode.Inactive, "Only active TAs can proctor.") };
        return EligibilityRules.Evaluate(profile, session, BookingsOf(userId), Rules);
    }

    /// <summary>Re-checks an existing assignment, ignoring the assignment itself.</summary>
    public IReadOnlyList<RuleViolation> Revalidate(Guid userId, QuizSession session)
    {
        if (!Profiles.TryGetValue(userId, out var profile))
            return Array.Empty<RuleViolation>();
        var others = BookingsOf(userId).Where(b => b.QuizId != session.QuizId);
        return EligibilityRules.Evaluate(profile, session, others, Rules);
    }

    public void Book(Guid userId, QuizSession session, Guid locationId, double points = 1)
    {
        if (!Ledger.TryGetValue(userId, out var list))
        {
            list = new List<BookedSession>();
            Ledger[userId] = list;
        }
        list.Add(new BookedSession(session.QuizId, locationId, session.SemesterId, session.Date,
            session.Start, session.End, session.WeekKey, points));
    }

    public void Unbook(Guid userId, Guid locationId)
    {
        if (Ledger.TryGetValue(userId, out var list))
            list.RemoveAll(b => b.LocationId == locationId);
    }
}

public sealed class AssignmentWorkspace
{
    private readonly IApplicationDbContext _db;

    public AssignmentWorkspace(IApplicationDbContext db) => _db = db;

    public async Task<AssignmentContext> LoadAsync(Guid semesterId, CancellationToken ct)
    {
        var semester = await _db.Semesters.FirstAsync(s => s.Id == semesterId, ct);
        var settings = await _db.GetSettingsAsync(ct);

        var tas = await _db.Users.AsNoTracking()
            .Where(u => u.Role == UserRole.TA && !u.IsDeleted)
            .ToListAsync(ct);

        var slots = await _db.TAScheduleSlots.AsNoTracking()
            .Where(s => s.SemesterId == semesterId)
            .ToListAsync(ct);

        var excuses = await _db.Excuses.AsNoTracking()
            .Where(e => e.EndDate >= semester.StartDate && e.StartDate <= semester.EndDate)
            .ToListAsync(ct);

        var bookings = await _db.ProctorAssignments.AsNoTracking()
            .Where(a => a.QuizLocation.Quiz.SemesterId == semesterId)
            .Select(a => new
            {
                a.UserId,
                a.QuizLocationId,
                a.Points,
                QuizId = a.QuizLocation.QuizId,
                a.QuizLocation.Quiz.QuizDate,
                a.QuizLocation.Quiz.StartTime,
                a.QuizLocation.Quiz.EndTime
            })
            .ToListAsync(ct);

        var slotsByUser = slots.ToLookup(s => s.UserId);
        var excusesByUser = excuses.ToLookup(e => e.UserId);

        var profiles = tas.ToDictionary(u => u.Id, u => new ProctorProfile
        {
            Id = u.Id,
            Name = u.FullName,
            IsActive = u.IsActive,
            DaysOff = u.DaysOff,
            MaxSessionsPerSemester = u.MaxSessionsOverride ?? settings.DefaultMaxSessionsPerSemester,
            TargetWorkload = u.TargetWorkloadOverride ?? settings.DefaultTargetWorkload,
            Teaching = slotsByUser[u.Id]
                .Select(s => new TeachingCommitment(s.DayOfWeek, s.SlotNumber, s.StartTime, s.EndTime, s.CourseName, s.SlotType))
                .ToList(),
            Excuses = excusesByUser[u.Id]
                .Select(e => new UnavailablePeriod(e.StartDate, e.EndDate, e.Reason))
                .ToList(),
            CourseIds = slotsByUser[u.Id].Where(s => s.CourseId.HasValue).Select(s => s.CourseId!.Value).ToHashSet()
        });

        var ledger = bookings
            .GroupBy(b => b.UserId)
            .ToDictionary(g => g.Key, g => g.Select(b => new BookedSession(
                b.QuizId, b.QuizLocationId, semesterId, b.QuizDate, b.StartTime, b.EndTime,
                AcademicCalendar.WeekNumber(semester.StartDate, b.QuizDate), b.Points)).ToList());

        return new AssignmentContext
        {
            Semester = semester,
            Settings = settings,
            Rules = new RulesConfig(settings.MaxSessionsPerWeek),
            Profiles = profiles,
            Ledger = ledger
        };
    }

    /// <summary>Turns quizzes (with locations and assignments loaded) into engine demands.</summary>
    public static List<QuizDemand> BuildDemands(AssignmentContext context, IEnumerable<Quiz> quizzes) =>
        quizzes.Select(q => new QuizDemand(
            context.SessionFor(q),
            q.Course?.Name ?? string.Empty,
            q.Locations.Select(l => new LocationDemand(
                l.Id,
                l.RoomName,
                l.IsReserve,
                l.SortOrder,
                Math.Max(0, l.ProctorsNeeded - l.Assignments.Count),
                l.Assignments.Any(a => a.IsCourseTa))).ToList()))
        .ToList();

    /// <summary>Loads quizzes ready for the engine.</summary>
    public IQueryable<Quiz> QuizzesWithStaffing() =>
        _db.Quizzes
            .Include(q => q.Course)
            .Include(q => q.Locations).ThenInclude(l => l.Assignments);
}
