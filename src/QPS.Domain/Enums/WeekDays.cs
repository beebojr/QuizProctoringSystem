namespace QPS.Domain.Enums;

/// <summary>
/// A set of weekdays stored as bit flags (used for a TA's extra days off).
/// Friday is always off for everyone and does not need to be stored.
/// </summary>
[Flags]
public enum WeekDays
{
    None = 0,
    Friday = 1 << 0,
    Saturday = 1 << 1,
    Sunday = 1 << 2,
    Monday = 1 << 3,
    Tuesday = 1 << 4,
    Wednesday = 1 << 5,
    Thursday = 1 << 6
}
