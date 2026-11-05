using System.Globalization;
using System.Text.RegularExpressions;
using QPS.Domain.Scheduling;

namespace QPS.Application.Common.Services;

/// <summary>Parsing helpers for the university quiz calendar (Excel or PDF).</summary>
public static class CalendarText
{
    private static readonly CultureInfo Inv = CultureInfo.InvariantCulture;

    private static readonly string[] DateFormats =
    {
        "yyyy-MM-dd", "yyyy/MM/dd",
        "MMMM d, yyyy", "MMMM dd, yyyy", "MMM d, yyyy", "MMM dd, yyyy",
        "MMMM d yyyy", "MMM d yyyy",
        "d MMMM yyyy", "dd MMMM yyyy", "d MMM yyyy", "dd MMM yyyy",
        "d MMMM, yyyy", "d MMM, yyyy",
        // Egypt writes day first.
        "d/M/yyyy", "dd/MM/yyyy", "d-M-yyyy", "dd-MM-yyyy", "d.M.yyyy", "dd.MM.yyyy"
    };

    private static readonly string[] DayNames =
        { "monday", "tuesday", "wednesday", "thursday", "friday", "saturday", "sunday",
          "mon", "tue", "tues", "wed", "thu", "thur", "thurs", "fri", "sat", "sun" };

    /// <summary>Understands "Tuesday, April 21, 2026", "April 21, 2026", "21/4/2026", "2026-04-21".</summary>
    public static bool TryParseDate(string? text, out DateOnly date)
    {
        date = default;
        if (string.IsNullOrWhiteSpace(text)) return false;
        var t = NameKey.Clean(text).Trim(',', ' ');

        // Strip a leading weekday ("Tuesday, " / "Tue ").
        var firstWord = t.Split(new[] { ' ', ',' }, StringSplitOptions.RemoveEmptyEntries).FirstOrDefault()?.ToLowerInvariant();
        if (firstWord is not null && DayNames.Contains(firstWord))
            t = t[(t.IndexOf(firstWord, StringComparison.OrdinalIgnoreCase) + firstWord.Length)..].TrimStart(',', ' ');

        // "21st April 2026" → "21 April 2026"
        t = Regex.Replace(t, @"\b(\d{1,2})(st|nd|rd|th)\b", "$1", RegexOptions.IgnoreCase);

        if (DateOnly.TryParseExact(t, DateFormats, Inv, DateTimeStyles.AllowWhiteSpaces, out date))
            return true;

        // Excel may hand over a full date-time ("2026-04-21 00:00:00").
        if (DateTime.TryParse(t, Inv, DateTimeStyles.AllowWhiteSpaces, out var dt))
        {
            date = DateOnly.FromDateTime(dt);
            return true;
        }
        return false;
    }

    private static readonly Regex TimeRange = new(@"(\d{1,2})[:.](\d{2})\s*(am|pm)?\s*[-–to]+\s*(\d{1,2})[:.](\d{2})\s*(am|pm)?",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    /// <summary>
    /// Slot label → times. Falls back to an explicit range such as "3:15-3:45" (hours before 8
    /// are treated as afternoon, because there are no classes before 8:30).
    /// </summary>
    public static bool TryParseSlot(string? text, out TimeOnly start, out TimeOnly end, out int? slotNumber)
    {
        start = default;
        end = default;
        slotNumber = null;
        if (SlotCatalog.TryParse(text, out var slot))
        {
            start = slot.Start;
            end = slot.End;
            slotNumber = slot.Number;
            return true;
        }
        if (string.IsNullOrWhiteSpace(text)) return false;
        var m = TimeRange.Match(text);
        if (!m.Success) return false;
        start = ToTime(int.Parse(m.Groups[1].Value), int.Parse(m.Groups[2].Value), m.Groups[3].Value);
        end = ToTime(int.Parse(m.Groups[4].Value), int.Parse(m.Groups[5].Value), m.Groups[6].Value);
        if (end <= start) return false;
        slotNumber = SlotCatalog.FromTimes(start, end)?.Number;
        return true;
    }

    public static bool TryParseTime(string? text, out TimeOnly time)
    {
        time = default;
        if (string.IsNullOrWhiteSpace(text)) return false;
        var m = Regex.Match(text, @"(\d{1,2})[:.](\d{2})\s*(am|pm)?", RegexOptions.IgnoreCase);
        if (!m.Success) return false;
        time = ToTime(int.Parse(m.Groups[1].Value), int.Parse(m.Groups[2].Value), m.Groups[3].Value);
        return true;
    }

    private static TimeOnly ToTime(int hour, int minute, string meridiem)
    {
        if (meridiem.Equals("pm", StringComparison.OrdinalIgnoreCase) && hour < 12) hour += 12;
        else if (meridiem.Equals("am", StringComparison.OrdinalIgnoreCase) && hour == 12) hour = 0;
        else if (meridiem.Length == 0 && hour < 8) hour += 12;
        return new TimeOnly(Math.Clamp(hour, 0, 23), Math.Clamp(minute, 0, 59));
    }

    public static int? ParseWeek(string? text)
    {
        if (string.IsNullOrWhiteSpace(text)) return null;
        var m = Regex.Match(text, @"\d+");
        return m.Success && int.TryParse(m.Value, out var w) && w is > 0 and < 60 ? w : null;
    }

    /// <summary>
    /// "C.101-C.102-C.201" → three rooms. Also handles "In the lecture C.201", commas,
    /// slashes, "&amp;" and "and".
    /// </summary>
    public static List<string> SplitRooms(string? text)
    {
        if (string.IsNullOrWhiteSpace(text)) return new List<string>();
        var parts = Regex.Split(text, @"\s*(?:-|–|,|/|;|&|\+|\band\b)\s*", RegexOptions.IgnoreCase);
        var rooms = new List<string>();
        foreach (var raw in parts)
        {
            var part = NameKey.Clean(raw);
            if (part.Length == 0) continue;
            // Phrases such as "In the lecture C.201" → keep the room code (the last token).
            if (part.Contains(' '))
            {
                var tokens = part.Split(' ');
                var code = tokens.LastOrDefault(t => Regex.IsMatch(t, @"\d"));
                part = code ?? part;
            }
            if (!rooms.Any(r => NameKey.For(r) == NameKey.For(part)))
                rooms.Add(part);
        }
        return rooms;
    }
}
