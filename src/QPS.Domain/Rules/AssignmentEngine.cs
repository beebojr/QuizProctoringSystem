namespace QPS.Domain.Rules;

public sealed record LocationDemand(
    Guid LocationId, string RoomName, bool IsReserve, int SortOrder, int Missing, bool HasCourseTa);

public sealed record QuizDemand(QuizSession Session, string CourseName, IReadOnlyList<LocationDemand> Locations);

public sealed record PlannedAssignment(Guid QuizId, Guid LocationId, Guid UserId, bool IsCourseTa);

public sealed record ShortfallReason(RuleCode Code, string Label, int Count);

public sealed record Shortfall(
    Guid QuizId, Guid LocationId, string RoomName, int Missing, IReadOnlyList<ShortfallReason> Reasons);

public sealed class AssignmentPlan
{
    public List<PlannedAssignment> Assignments { get; } = new();
    public List<Shortfall> Shortfalls { get; } = new();
}

public sealed record EngineOptions(RulesConfig Rules, bool PreferCourseTas);

/// <summary>
/// Batch assignment run. Quizzes are processed in chronological order and every empty seat is
/// filled from the TAs who pass every rule, preferring the TA furthest below their workload
/// target. It only fills missing seats: existing assignments are never moved.
/// </summary>
public static class AssignmentEngine
{
    public static AssignmentPlan Plan(
        IEnumerable<QuizDemand> demands,
        IReadOnlyList<ProctorProfile> pool,
        IReadOnlyDictionary<Guid, List<BookedSession>> existingBookings,
        EngineOptions options)
    {
        var plan = new AssignmentPlan();

        // Work on a copy so callers can reuse their ledger (e.g. preview then commit).
        var ledger = pool.ToDictionary(
            p => p.Id,
            p => existingBookings.TryGetValue(p.Id, out var list) ? new List<BookedSession>(list) : new List<BookedSession>());

        var ordered = demands
            .OrderBy(d => d.Session.Date)
            .ThenBy(d => d.Session.Start)
            .ThenBy(d => d.CourseName, StringComparer.OrdinalIgnoreCase)
            .ThenBy(d => d.Session.QuizId);

        foreach (var demand in ordered)
        {
            var quiz = demand.Session;
            var verdicts = pool.Select(ta => (ta, violations: EligibilityRules.Evaluate(ta, quiz, ledger[ta.Id], options.Rules)))
                .ToList();
            var eligible = verdicts.Where(v => v.violations.Count == 0).Select(v => v.ta).ToList();

            var seats = demand.Locations
                .Where(l => l.Missing > 0)
                .OrderBy(l => l.IsReserve)
                .ThenBy(l => l.SortOrder)
                .ThenBy(l => l.RoomName, StringComparer.OrdinalIgnoreCase)
                .Select(l => new SeatState(l))
                .ToList();
            var pickedHere = new HashSet<Guid>();

            void Take(SeatState seat, ProctorProfile ta)
            {
                var isCourseTa = ta.CourseIds.Contains(quiz.CourseId);
                plan.Assignments.Add(new PlannedAssignment(quiz.QuizId, seat.Location.LocationId, ta.Id, isCourseTa));
                ledger[ta.Id].Add(new BookedSession(
                    quiz.QuizId, seat.Location.LocationId, quiz.SemesterId, quiz.Date, quiz.Start, quiz.End, quiz.WeekKey, 1));
                eligible.Remove(ta); // one seat per TA per quiz
                pickedHere.Add(ta.Id);
                seat.HasCourseTa |= isCourseTa;
                seat.Missing--;
            }

            // Pass 1: spread the course's own TAs so each room gets one (when there are enough).
            if (options.PreferCourseTas)
            {
                foreach (var seat in seats.Where(s => !s.HasCourseTa))
                {
                    var courseTa = Rank(eligible.Where(t => t.CourseIds.Contains(quiz.CourseId)), quiz, ledger).FirstOrDefault();
                    if (courseTa is null) break;
                    Take(seat, courseTa);
                }
            }

            // Pass 2: fill every remaining seat by fairness.
            foreach (var seat in seats)
            {
                while (seat.Missing > 0 && eligible.Count > 0)
                    Take(seat, Rank(eligible, quiz, ledger).First());
            }

            foreach (var seat in seats.Where(s => s.Missing > 0))
            {
                var reasons = verdicts
                    .Where(v => v.violations.Count > 0 || pickedHere.Contains(v.ta.Id))
                    .GroupBy(v => pickedHere.Contains(v.ta.Id) ? RuleCode.AlreadyInQuiz : v.violations[0].Code)
                    .Select(g => new ShortfallReason(g.Key, EligibilityRules.Describe(g.Key), g.Count()))
                    .OrderByDescending(r => r.Count)
                    .ToList();
                plan.Shortfalls.Add(new Shortfall(quiz.QuizId, seat.Location.LocationId, seat.Location.RoomName, seat.Missing, reasons));
            }
        }

        return plan;
    }

    private sealed class SeatState
    {
        public SeatState(LocationDemand location)
        {
            Location = location;
            Missing = location.Missing;
            HasCourseTa = location.HasCourseTa;
        }

        public LocationDemand Location { get; }
        public int Missing { get; set; }
        public bool HasCourseTa { get; set; }
    }

    /// <summary>
    /// Fairness order: lowest share of workload target first, then fewest sessions this week,
    /// then fewest sessions this semester. Name/Id make the result deterministic.
    /// </summary>
    public static IEnumerable<ProctorProfile> Rank(
        IEnumerable<ProctorProfile> candidates,
        QuizSession quiz,
        IReadOnlyDictionary<Guid, List<BookedSession>> ledger)
    {
        return candidates
            .Select(ta =>
            {
                var semester = ledger.TryGetValue(ta.Id, out var list)
                    ? list.Where(b => b.SemesterId == quiz.SemesterId).ToList()
                    : new List<BookedSession>();
                var points = semester.Sum(b => b.Points);
                var ratio = points / Math.Max(ta.TargetWorkload, 1);
                var week = semester.Count(b => b.WeekKey == quiz.WeekKey);
                return (ta, ratio, week, count: semester.Count);
            })
            .OrderBy(x => x.ratio)
            .ThenBy(x => x.week)
            .ThenBy(x => x.count)
            .ThenBy(x => x.ta.Name, StringComparer.OrdinalIgnoreCase)
            .ThenBy(x => x.ta.Id)
            .Select(x => x.ta);
    }
}
