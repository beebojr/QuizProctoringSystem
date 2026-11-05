using System.Globalization;
using QPS.Domain.Enums;
using QPS.Domain.Scheduling;

namespace QPS.Application.Common.Services;

public static class Format
{
    private static readonly CultureInfo Inv = CultureInfo.InvariantCulture;

    public static string Date(DateOnly date) => date.ToString("ddd d MMM", Inv);
    public static string LongDate(DateOnly date) => date.ToString("dddd, MMMM d, yyyy", Inv);
    public static string Time(TimeOnly time) => time.ToString("h:mm tt", Inv);
    public static string TimeRange(TimeOnly start, TimeOnly end) => $"{Time(start)} – {Time(end)}";

    public static List<string> Days(WeekDays days) =>
        AcademicCalendar.FromFlags(days).Select(d => d.ToString()).ToList();

    public static string SlotName(int slotNumber) =>
        slotNumber == 0 ? "the gap slot" : $"Slot {slotNumber}";
}
