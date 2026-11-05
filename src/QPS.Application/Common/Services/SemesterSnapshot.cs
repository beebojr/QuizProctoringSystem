using Microsoft.EntityFrameworkCore;
using QPS.Application.Common.Interfaces;
using QPS.Domain.Entities;
using QPS.Domain.Enums;
using QPS.Domain.Scheduling;

namespace QPS.Application.Common.Services;

/// <summary>A read-only, in-memory picture of one semester used by dashboards and reports.</summary>
public sealed class SemesterSnapshot
{
    public required Semester Semester { get; init; }
    public required SystemSettings Settings { get; init; }
    public required DateTime Now { get; init; }
    public required List<Quiz> Quizzes { get; init; }
    public required List<User> Tas { get; init; }
    public required List<TAScheduleSlot> Slots { get; init; }
    public required List<Excuse> Excuses { get; init; }

    public DateOnly Today => DateOnly.FromDateTime(Now);

    public IEnumerable<(Quiz Quiz, QuizLocation Location, ProctorAssignment Assignment)> Assignments =>
        Quizzes.SelectMany(q => q.Locations.SelectMany(l => l.Assignments.Select(a => (q, l, a))));

    public QuizStatus StatusOf(Quiz q) => QuizTiming.StatusAt(q.QuizDate, q.StartTime, q.EndTime, Now);

    public int CurrentWeek => AcademicCalendar.WeekNumber(Semester.StartDate, Today);

    public int MaxFor(User u) => u.MaxSessionsOverride ?? Settings.DefaultMaxSessionsPerSemester;
    public double TargetFor(User u) => u.TargetWorkloadOverride ?? Settings.DefaultTargetWorkload;

    public static async Task<SemesterSnapshot> LoadAsync(IApplicationDbContext db, Guid? semesterId, DateTime now, CancellationToken ct)
    {
        var semester = await db.ResolveSemesterAsync(semesterId, ct);
        var settings = await db.GetSettingsAsync(ct);
        var quizzes = await db.Quizzes.AsNoTracking()
            .Include(q => q.Course)
            .Include(q => q.Locations).ThenInclude(l => l.Assignments).ThenInclude(a => a.User)
            .Where(q => q.SemesterId == semester.Id)
            .ToListAsync(ct);
        var tas = await db.Users.AsNoTracking().Where(u => u.Role == UserRole.TA && !u.IsDeleted).ToListAsync(ct);
        var slots = await db.TAScheduleSlots.AsNoTracking().Where(s => s.SemesterId == semester.Id).ToListAsync(ct);
        var excuses = await db.Excuses.AsNoTracking().ToListAsync(ct);
        return new SemesterSnapshot
        {
            Semester = semester, Settings = settings, Now = now, Quizzes = quizzes, Tas = tas, Slots = slots, Excuses = excuses
        };
    }
}
