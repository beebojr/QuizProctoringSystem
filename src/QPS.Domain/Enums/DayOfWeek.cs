namespace QPS.Domain.Enums;

/// <summary>
/// University week order (the week starts on Saturday in Egypt; Friday is the weekend).
/// Values are kept from the original design so existing data stays compatible.
/// </summary>
public enum DayOfWeek
{
    Friday = 0,
    Saturday = 1,
    Sunday = 2,
    Monday = 3,
    Tuesday = 4,
    Wednesday = 5,
    Thursday = 6
}
