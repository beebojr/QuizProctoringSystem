using QPS.Domain.Enums;
using QPS.Domain.Rules;
using QPS.Domain.Scheduling;
using DayOfWeek = QPS.Domain.Enums.DayOfWeek;

namespace QPS.Tests;

public static class RuleTests
{
    // Semester week 1 starts Tuesday 2026-03-10, like the university calendar (Tue → Mon weeks).
    private static readonly DateOnly Start = new(2026, 3, 10);
    private static readonly Guid Semester = Guid.NewGuid();
    private static readonly Guid Course = Guid.NewGuid();
    private static readonly RulesConfig Rules = new(MaxSessionsPerWeek: 2);

    private static ProctorProfile Ta(string name = "TA", WeekDays off = WeekDays.None, int max = 10, double target = 14,
        IEnumerable<TeachingCommitment>? teaching = null, IEnumerable<UnavailablePeriod>? excuses = null,
        bool active = true, IEnumerable<Guid>? courses = null) => new()
    {
        Id = Guid.NewGuid(),
        Name = name,
        IsActive = active,
        DaysOff = off,
        MaxSessionsPerSemester = max,
        TargetWorkload = target,
        Teaching = (teaching ?? Array.Empty<TeachingCommitment>()).ToList(),
        Excuses = (excuses ?? Array.Empty<UnavailablePeriod>()).ToList(),
        CourseIds = (courses ?? Array.Empty<Guid>()).ToHashSet()
    };

    private static QuizSession Quiz(DateOnly date, int slot = 5, Guid? id = null)
    {
        var s = SlotCatalog.Get(slot)!;
        return new QuizSession(id ?? Guid.NewGuid(), Semester, Course, date, s.Start, s.End, AcademicCalendar.WeekNumber(Start, date));
    }

    private static BookedSession Booked(QuizSession q) =>
        new(q.QuizId, Guid.NewGuid(), q.SemesterId, q.Date, q.Start, q.End, q.WeekKey, 1);

    private static IReadOnlyList<RuleCode> Codes(ProctorProfile ta, QuizSession q, params BookedSession[] booked) =>
        EligibilityRules.Evaluate(ta, q, booked, Rules).Select(v => v.Code).ToList();

    [Test]
    public static void Free_ta_is_eligible() =>
        Assert.Equal(0, Codes(Ta(), Quiz(new DateOnly(2026, 4, 28))).Count, "violations");

    [Test]
    public static void Rule1_semester_limit_blocks_the_next_session()
    {
        var ta = Ta(max: 3);
        var booked = new[] { new DateOnly(2026, 3, 11), new DateOnly(2026, 3, 18), new DateOnly(2026, 3, 25) }
            .Select(d => Booked(Quiz(d))).ToArray();
        Assert.True(Codes(ta, Quiz(new DateOnly(2026, 4, 28)), booked).Contains(RuleCode.SemesterLimit), "Rule 1 should block");
        Assert.True(!Codes(ta, Quiz(new DateOnly(2026, 4, 28)), booked.Take(2).ToArray()).Contains(RuleCode.SemesterLimit), "2 of 3 is fine");
    }

    [Test]
    public static void Rule2_third_session_in_the_same_academic_week_is_blocked()
    {
        var ta = Ta();
        // Week 8 runs Tue 28 Apr → Mon 4 May.
        var a = Booked(Quiz(new DateOnly(2026, 4, 28)));
        var b = Booked(Quiz(new DateOnly(2026, 4, 30)));
        Assert.True(Codes(ta, Quiz(new DateOnly(2026, 5, 4)), a, b).Contains(RuleCode.WeeklyLimit), "Monday is still week 8");
        Assert.True(!Codes(ta, Quiz(new DateOnly(2026, 5, 5)), a, b).Contains(RuleCode.WeeklyLimit), "Tuesday starts week 9");
    }

    [Test]
    public static void Rule3_teaching_slot_overlap_is_blocked_but_adjacent_gap_is_allowed()
    {
        // Tuesday 4th-slot teaching (13:45–15:15). The 3:15–3:45 gap quiz touches but does not overlap.
        var ta = Ta(teaching: new[] { new TeachingCommitment(DayOfWeek.Tuesday, 4, new TimeOnly(13, 45), new TimeOnly(15, 15), "Networks", SlotType.Lab) });
        var tuesday = new DateOnly(2026, 4, 28);
        Assert.True(Codes(ta, Quiz(tuesday, 4)).Contains(RuleCode.TeachingConflict), "same slot conflicts");
        Assert.Equal(0, Codes(ta, Quiz(tuesday, 0)).Count, "gap right after the slot");
        Assert.Equal(0, Codes(ta, Quiz(new DateOnly(2026, 4, 29), 4)).Count, "other weekday is free");
        var midterm = new QuizSession(Guid.NewGuid(), Semester, Course, tuesday, new TimeOnly(15, 0), new TimeOnly(17, 0), 8);
        Assert.True(Codes(ta, midterm).Contains(RuleCode.TeachingConflict), "partial overlap conflicts");
    }

    [Test]
    public static void Friday_and_personal_days_off_are_blocked()
    {
        var ta = Ta(off: WeekDays.Sunday | WeekDays.Wednesday);
        Assert.True(Codes(ta, Quiz(new DateOnly(2026, 5, 1))).Contains(RuleCode.FridayOff), "Friday");
        Assert.True(Codes(ta, Quiz(new DateOnly(2026, 5, 3))).Contains(RuleCode.DayOff), "Sunday off");
        Assert.True(Codes(ta, Quiz(new DateOnly(2026, 4, 29))).Contains(RuleCode.DayOff), "Wednesday off");
        Assert.Equal(0, Codes(ta, Quiz(new DateOnly(2026, 4, 28))).Count, "Tuesday works");
    }

    [Test]
    public static void Excuse_period_is_inclusive()
    {
        var ta = Ta(excuses: new[] { new UnavailablePeriod(new DateOnly(2026, 5, 3), new DateOnly(2026, 5, 5), "Sick") });
        Assert.True(Codes(ta, Quiz(new DateOnly(2026, 5, 3))).Contains(RuleCode.Excused), "first day");
        Assert.True(Codes(ta, Quiz(new DateOnly(2026, 5, 5))).Contains(RuleCode.Excused), "last day");
        Assert.Equal(0, Codes(ta, Quiz(new DateOnly(2026, 5, 6))).Count, "day after");
    }

    [Test]
    public static void Double_booking_and_same_quiz_are_blocked()
    {
        var ta = Ta();
        var q = Quiz(new DateOnly(2026, 4, 28), 5);
        Assert.True(Codes(ta, q, Booked(q)).Contains(RuleCode.AlreadyInQuiz), "same quiz");
        var other = Quiz(new DateOnly(2026, 4, 28), 5);
        Assert.True(Codes(ta, other, Booked(q)).Contains(RuleCode.DoubleBooked), "same time other quiz");
    }

    [Test]
    public static void Inactive_ta_is_blocked() =>
        Assert.True(Codes(Ta(active: false), Quiz(new DateOnly(2026, 4, 28))).Contains(RuleCode.Inactive), "inactive");

    [Test]
    public static void Engine_never_breaks_a_rule_and_spreads_the_load()
    {
        var pool = Enumerable.Range(0, 8).Select(i => Ta($"TA{i}", max: 6)).ToList();
        var demands = new List<QuizDemand>();
        for (var w = 0; w < 4; w++)
        for (var d = 0; d < 3; d++)
        {
            var date = Start.AddDays(w * 7 + d);
            if (date.DayOfWeek == System.DayOfWeek.Friday) continue;
            var q = Quiz(date, 5);
            demands.Add(new QuizDemand(q, "Course", new[] { new LocationDemand(Guid.NewGuid(), "C.101", false, 0, 3, false) }));
        }

        var plan = AssignmentEngine.Plan(demands, pool, new Dictionary<Guid, List<BookedSession>>(), new EngineOptions(Rules, true));
        var sessions = demands.ToDictionary(d => d.Session.QuizId, d => d.Session);
        foreach (var group in plan.Assignments.GroupBy(a => a.UserId))
        {
            var mine = group.Select(a => sessions[a.QuizId]).ToList();
            Assert.True(mine.GroupBy(s => s.WeekKey).All(g => g.Count() <= 2), "weekly limit");
            Assert.True(mine.Count <= 6, "semester limit");
            Assert.True(mine.Select(s => s.QuizId).Distinct().Count() == mine.Count, "one seat per quiz");
        }
        var counts = pool.Select(p => plan.Assignments.Count(a => a.UserId == p.Id)).ToList();
        Assert.True(counts.Max() - counts.Min() <= 1, $"fair spread, got {string.Join(",", counts)}");
        Assert.Equal(0, plan.Shortfalls.Count, "everything staffed");
    }

    [Test]
    public static void Engine_puts_a_course_ta_in_each_room_when_possible()
    {
        var courseTa1 = Ta("Course TA 1", courses: new[] { Course });
        var courseTa2 = Ta("Course TA 2", courses: new[] { Course });
        var others = Enumerable.Range(0, 4).Select(i => Ta($"Other {i}")).ToList();
        var pool = others.Concat(new[] { courseTa1, courseTa2 }).ToList();
        var q = Quiz(new DateOnly(2026, 4, 28));
        var demand = new QuizDemand(q, "Course", new[]
        {
            new LocationDemand(Guid.NewGuid(), "A", false, 0, 2, false),
            new LocationDemand(Guid.NewGuid(), "B", false, 1, 2, false),
        });
        var plan = AssignmentEngine.Plan(new[] { demand }, pool, new Dictionary<Guid, List<BookedSession>>(), new EngineOptions(Rules, true));
        foreach (var loc in demand.Locations)
            Assert.Equal(1, plan.Assignments.Count(a => a.LocationId == loc.LocationId && a.IsCourseTa), $"course TA in {loc.RoomName}");
    }

    [Test]
    public static void Engine_reports_why_a_room_is_short()
    {
        var pool = new[] { Ta("Off on Tuesday", off: WeekDays.Tuesday), Ta("Free") };
        var q = Quiz(new DateOnly(2026, 4, 28));
        var plan = AssignmentEngine.Plan(new[] { new QuizDemand(q, "Course", new[] { new LocationDemand(Guid.NewGuid(), "A", false, 0, 2, false) }) },
            pool, new Dictionary<Guid, List<BookedSession>>(), new EngineOptions(Rules, true));
        Assert.Equal(1, plan.Assignments.Count, "one TA could go");
        Assert.Equal(1, plan.Shortfalls.Single().Missing, "one seat missing");
        Assert.True(plan.Shortfalls.Single().Reasons.Any(r => r.Code == RuleCode.DayOff), "reason is the day off");
    }

    [Test]
    public static void Engine_keeps_existing_assignments_and_only_fills_missing_seats()
    {
        var ta = Ta("Already booked");
        var q = Quiz(new DateOnly(2026, 4, 28));
        var location = Guid.NewGuid();
        var existing = new Dictionary<Guid, List<BookedSession>> { [ta.Id] = new() { new BookedSession(q.QuizId, location, Semester, q.Date, q.Start, q.End, q.WeekKey, 1) } };
        var other = Ta("Other");
        var plan = AssignmentEngine.Plan(new[] { new QuizDemand(q, "Course", new[] { new LocationDemand(location, "A", false, 0, 1, false) }) },
            new[] { ta, other }, existing, new EngineOptions(Rules, true));
        Assert.Equal(other.Id, plan.Assignments.Single().UserId, "only the free TA is added");
        Assert.Equal(1, existing[ta.Id].Count, "caller's ledger untouched");
    }

    [Test]
    public static void Academic_weeks_follow_the_semester_start_weekday()
    {
        Assert.Equal(7, AcademicCalendar.WeekNumber(Start, new DateOnly(2026, 4, 21)), "Tue 21 Apr");
        Assert.Equal(7, AcademicCalendar.WeekNumber(Start, new DateOnly(2026, 4, 27)), "Mon 27 Apr");
        Assert.Equal(8, AcademicCalendar.WeekNumber(Start, new DateOnly(2026, 4, 28)), "Tue 28 Apr");
    }
}
