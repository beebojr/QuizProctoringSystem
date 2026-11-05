using QPS.Domain.Enums;
using DomainDay = QPS.Domain.Enums.DayOfWeek;

namespace QPS.Domain.Rules;

/// <summary>Why a TA cannot proctor a given quiz.</summary>
public enum RuleCode
{
    Inactive,
    FridayOff,
    DayOff,
    Excused,
    /// <summary>Rule 3: the quiz overlaps a slot the TA teaches.</summary>
    TeachingConflict,
    AlreadyInQuiz,
    DoubleBooked,
    /// <summary>Rule 2: maximum sessions per week reached.</summary>
    WeeklyLimit,
    /// <summary>Rule 1: maximum sessions per semester reached.</summary>
    SemesterLimit
}

public sealed record RuleViolation(RuleCode Code, string Message);

public sealed record TeachingCommitment(
    DomainDay Day, int SlotNumber, TimeOnly Start, TimeOnly End, string CourseName, SlotType Type);

public sealed record UnavailablePeriod(DateOnly Start, DateOnly End, string Reason)
{
    public bool Contains(DateOnly date) => date >= Start && date <= End;
}

/// <summary>Everything the rules need to know about one TA for one semester.</summary>
public sealed class ProctorProfile
{
    public required Guid Id { get; init; }
    public required string Name { get; init; }
    public bool IsActive { get; init; } = true;
    public WeekDays DaysOff { get; init; }
    public int MaxSessionsPerSemester { get; init; }
    public double TargetWorkload { get; init; }
    public IReadOnlyList<TeachingCommitment> Teaching { get; init; } = Array.Empty<TeachingCommitment>();
    public IReadOnlyList<UnavailablePeriod> Excuses { get; init; } = Array.Empty<UnavailablePeriod>();
    public IReadOnlySet<Guid> CourseIds { get; init; } = new HashSet<Guid>();
}

/// <summary>A quiz sitting being staffed. WeekKey is the academic week inside the semester.</summary>
public sealed record QuizSession(
    Guid QuizId, Guid SemesterId, Guid CourseId, DateOnly Date, TimeOnly Start, TimeOnly End, int WeekKey);

/// <summary>A session a TA is already booked for.</summary>
public sealed record BookedSession(
    Guid QuizId, Guid LocationId, Guid SemesterId, DateOnly Date, TimeOnly Start, TimeOnly End, int WeekKey,
    double Points);

public sealed record RulesConfig(int MaxSessionsPerWeek);
