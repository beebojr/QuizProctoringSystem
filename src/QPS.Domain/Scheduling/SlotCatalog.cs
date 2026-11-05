using System.Text.RegularExpressions;

namespace QPS.Domain.Scheduling;

public sealed record SlotDefinition(int Number, string Label, TimeOnly Start, TimeOnly End)
{
    public bool IsGap => Number == GapSlotNumber;
    public const int GapSlotNumber = 0;
}

/// <summary>
/// The university's fixed daily slots. Slot 0 is the 3:15-3:45 gap used only for quizzes.
/// </summary>
public static class SlotCatalog
{
    public static readonly IReadOnlyList<SlotDefinition> TeachingSlots = new[]
    {
        new SlotDefinition(1, "1ST", new TimeOnly(8, 30), new TimeOnly(10, 0)),
        new SlotDefinition(2, "2ND", new TimeOnly(10, 15), new TimeOnly(11, 45)),
        new SlotDefinition(3, "3RD", new TimeOnly(12, 0), new TimeOnly(13, 30)),
        new SlotDefinition(4, "4TH", new TimeOnly(13, 45), new TimeOnly(15, 15)),
        new SlotDefinition(5, "5TH", new TimeOnly(15, 45), new TimeOnly(17, 15)),
    };

    public static readonly SlotDefinition Gap =
        new(SlotDefinition.GapSlotNumber, "GAP", new TimeOnly(15, 15), new TimeOnly(15, 45));

    /// <summary>All slots a quiz can use, in time order.</summary>
    public static readonly IReadOnlyList<SlotDefinition> QuizSlots =
        TeachingSlots.Take(4).Append(Gap).Append(TeachingSlots[4]).ToList();

    public static SlotDefinition? Get(int number) =>
        number == SlotDefinition.GapSlotNumber ? Gap : TeachingSlots.FirstOrDefault(s => s.Number == number);

    public static bool IsTeachingSlot(int number) => number is >= 1 and <= 5;

    /// <summary>Returns the slot whose times match exactly, or null for custom times.</summary>
    public static SlotDefinition? FromTimes(TimeOnly start, TimeOnly end) =>
        QuizSlots.FirstOrDefault(s => s.Start == start && s.End == end);

    private static readonly Regex OrdinalPattern = new(@"\b([1-5])\s*(ST|ND|RD|TH)\b", RegexOptions.Compiled);
    private static readonly Regex SlotWordPattern = new(@"\bSLOT\s*([1-5])\b", RegexOptions.Compiled);
    private static readonly Regex BareDigitPattern = new(@"^\s*([1-5])\s*$", RegexOptions.Compiled);

    private static readonly Dictionary<string, int> Words = new()
    {
        ["FIRST"] = 1, ["SECOND"] = 2, ["THIRD"] = 3, ["FOURTH"] = 4, ["FIFTH"] = 5
    };

    /// <summary>
    /// Understands the labels used in the university calendar: "1ST", "5th", "Slot 3",
    /// "Gap (3:15-3:45)", "gap", "first", "2".
    /// </summary>
    public static bool TryParse(string? text, out SlotDefinition slot)
    {
        slot = null!;
        if (string.IsNullOrWhiteSpace(text)) return false;
        var upper = text.Trim().ToUpperInvariant();

        if (upper.Contains("GAP"))
        {
            slot = Gap;
            return true;
        }

        var match = OrdinalPattern.Match(upper);
        if (!match.Success) match = SlotWordPattern.Match(upper);
        if (!match.Success) match = BareDigitPattern.Match(upper);
        if (match.Success)
        {
            slot = Get(int.Parse(match.Groups[1].Value))!;
            return true;
        }

        foreach (var (word, number) in Words)
        {
            if (Regex.IsMatch(upper, $@"\b{word}\b"))
            {
                slot = Get(number)!;
                return true;
            }
        }

        return false;
    }

    public static string Describe(int? slotNumber) =>
        slotNumber is null ? "Custom" : Get(slotNumber.Value)?.Label ?? "Custom";
}
