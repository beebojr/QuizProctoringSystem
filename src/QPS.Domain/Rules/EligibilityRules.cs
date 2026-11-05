using System.Globalization;
using QPS.Domain.Scheduling;

namespace QPS.Domain.Rules;

/// <summary>
/// The single place where the proctoring rules live. Auto-assignment, manual assignment,
/// the candidate picker and re-validation after edits all call this.
/// </summary>
public static class EligibilityRules
{
    public static IReadOnlyList<RuleViolation> Evaluate(
        ProctorProfile ta,
        QuizSession quiz,
        IEnumerable<BookedSession> booked,
        RulesConfig config)
    {
        var violations = new List<RuleViolation>();
        var bookedList = booked as IReadOnlyCollection<BookedSession> ?? booked.ToList();
        var day = AcademicCalendar.ToDomainDay(quiz.Date);

        if (!ta.IsActive)
            violations.Add(new(RuleCode.Inactive, "Account is deactivated."));

        if (day == Enums.DayOfWeek.Friday)
            violations.Add(new(RuleCode.FridayOff, "Friday is a day off for all TAs."));
        else if (ta.DaysOff.HasFlag(AcademicCalendar.ToFlag(day)))
            violations.Add(new(RuleCode.DayOff, $"{day} is {ta.Name}'s day off."));

        var excuse = ta.Excuses.FirstOrDefault(e => e.Contains(quiz.Date));
        if (excuse is not null)
        {
            var period = excuse.Start.ToString("d MMM", CultureInfo.InvariantCulture) + " – " + excuse.End.ToString("d MMM", CultureInfo.InvariantCulture);
            var reason = string.IsNullOrWhiteSpace(excuse.Reason) ? "." : " (" + excuse.Reason + ").";
            violations.Add(new(RuleCode.Excused, "Excused " + period + reason));
        }

        // Rule 3: no collision with the TA's regular (teaching) schedule.
        foreach (var slot in ta.Teaching.Where(t => t.Day == day))
        {
            if (QuizTiming.Overlaps(slot.Start, slot.End, quiz.Start, quiz.End))
            {
                violations.Add(new(RuleCode.TeachingConflict,
                    $"Teaches {slot.CourseName} ({slot.Type}) in slot {slot.SlotNumber} " +
                    $"({slot.Start:HH\\:mm}–{slot.End:HH\\:mm})."));
            }
        }

        if (bookedList.Any(b => b.QuizId == quiz.QuizId))
        {
            violations.Add(new(RuleCode.AlreadyInQuiz, "Already proctoring this quiz."));
        }
        else
        {
            var clash = bookedList.FirstOrDefault(b =>
                b.Date == quiz.Date && QuizTiming.Overlaps(b.Start, b.End, quiz.Start, quiz.End));
            if (clash is not null)
                violations.Add(new(RuleCode.DoubleBooked,
                    $"Already proctoring another quiz at {clash.Start:HH\\:mm}–{clash.End:HH\\:mm} that day."));
        }

        var sameSemester = bookedList.Where(b => b.SemesterId == quiz.SemesterId && b.QuizId != quiz.QuizId).ToList();

        // Rule 2: at most N sessions per academic week.
        var weekCount = sameSemester.Count(b => b.WeekKey == quiz.WeekKey);
        if (weekCount >= config.MaxSessionsPerWeek)
            violations.Add(new(RuleCode.WeeklyLimit,
                $"Already has {weekCount} session{(weekCount == 1 ? "" : "s")} that week " +
                $"(limit {config.MaxSessionsPerWeek})."));

        // Rule 1: at most x sessions per semester.
        var semesterCount = sameSemester.Count;
        if (semesterCount >= ta.MaxSessionsPerSemester)
            violations.Add(new(RuleCode.SemesterLimit,
                $"Reached the semester limit ({semesterCount} of {ta.MaxSessionsPerSemester})."));

        return violations;
    }

    public static string Describe(RuleCode code) => code switch
    {
        RuleCode.Inactive => "deactivated",
        RuleCode.FridayOff => "off on Fridays",
        RuleCode.DayOff => "on their day off",
        RuleCode.Excused => "excused",
        RuleCode.TeachingConflict => "teaching at that time",
        RuleCode.AlreadyInQuiz => "already proctoring this quiz",
        RuleCode.DoubleBooked => "proctoring another quiz then",
        RuleCode.WeeklyLimit => "at the weekly limit",
        RuleCode.SemesterLimit => "at the semester limit",
        _ => code.ToString()
    };
}
