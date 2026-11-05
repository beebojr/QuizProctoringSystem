namespace QPS.Domain.Enums;

/// <summary>
/// Computed from the quiz date/time and the current time (never stored).
/// </summary>
public enum QuizStatus
{
    Upcoming = 0,
    Started = 1,
    Finished = 2
}
