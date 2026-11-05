using System.Text;

namespace QPS.Domain.Scheduling;

/// <summary>
/// Normalises names so "Computer organization", "Computer  Organization " and
/// "COMPUTER ORGANIZATION" are treated as the same course or room.
/// </summary>
public static class NameKey
{
    public static string For(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return string.Empty;
        var sb = new StringBuilder(value.Length);
        var lastWasSpace = false;
        foreach (var ch in value.Trim().ToLowerInvariant())
        {
            var c = ch is ' ' or '\t' or '\r' or '\n' ? ' ' : ch;
            if (c == ' ')
            {
                if (lastWasSpace) continue;
                lastWasSpace = true;
            }
            else
            {
                lastWasSpace = false;
            }
            sb.Append(c);
        }
        return sb.ToString();
    }

    /// <summary>Collapses whitespace but keeps the original casing for display.</summary>
    public static string Clean(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return string.Empty;
        return string.Join(' ', value.Split(new[] { ' ', '\t', '\r', '\n', ' ' },
            StringSplitOptions.RemoveEmptyEntries));
    }
}
