using DomainDay = QPS.Domain.Enums.DayOfWeek;
using QPS.Domain.Enums;

namespace QPS.Domain.Scheduling;

public static class AcademicCalendar
{
    /// <summary>
    /// Academic week number (1-based). Week 1 starts on the semester start date and every
    /// week is 7 days long, so a semester starting on a Tuesday has Tuesday-Monday weeks,
    /// exactly like the university quiz calendar.
    /// </summary>
    public static int WeekNumber(DateOnly semesterStart, DateOnly date)
    {
        var days = date.DayNumber - semesterStart.DayNumber;
        return (int)Math.Floor(days / 7.0) + 1;
    }

    /// <summary>First day of the given academic week.</summary>
    public static DateOnly WeekStart(DateOnly semesterStart, int weekNumber) =>
        semesterStart.AddDays((weekNumber - 1) * 7);

    public static DomainDay ToDomainDay(DateOnly date) => ToDomainDay(date.DayOfWeek);

    public static DomainDay ToDomainDay(System.DayOfWeek day) => day switch
    {
        System.DayOfWeek.Friday => DomainDay.Friday,
        System.DayOfWeek.Saturday => DomainDay.Saturday,
        System.DayOfWeek.Sunday => DomainDay.Sunday,
        System.DayOfWeek.Monday => DomainDay.Monday,
        System.DayOfWeek.Tuesday => DomainDay.Tuesday,
        System.DayOfWeek.Wednesday => DomainDay.Wednesday,
        System.DayOfWeek.Thursday => DomainDay.Thursday,
        _ => throw new ArgumentOutOfRangeException(nameof(day))
    };

    public static WeekDays ToFlag(DomainDay day) => (WeekDays)(1 << (int)day);

    public static IReadOnlyList<DomainDay> FromFlags(WeekDays days) =>
        Enum.GetValues<DomainDay>().Where(d => days.HasFlag(ToFlag(d))).ToList();

    public static WeekDays ToFlags(IEnumerable<DomainDay> days) =>
        days.Aggregate(WeekDays.None, (acc, d) => acc | ToFlag(d));

    /// <summary>Saturday-first order used across the university.</summary>
    public static readonly IReadOnlyList<DomainDay> WorkingDays = new[]
    {
        DomainDay.Saturday, DomainDay.Sunday, DomainDay.Monday,
        DomainDay.Tuesday, DomainDay.Wednesday, DomainDay.Thursday
    };
}
