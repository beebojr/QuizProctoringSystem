using DayOfWeek = QPS.Domain.Enums.DayOfWeek;

namespace QPS.Application.Common.Services;

public static class DayParser
{
    public static bool TryParse(string? text, out DayOfWeek day)
    {
        day = default;
        if (string.IsNullOrWhiteSpace(text)) return false;
        var t = text.Trim().ToLowerInvariant();
        foreach (var d in Enum.GetValues<DayOfWeek>())
        {
            var name = d.ToString().ToLowerInvariant();
            if (t == name || (t.Length >= 3 && name.StartsWith(t)))
            {
                day = d;
                return true;
            }
        }
        return false;
    }
}
