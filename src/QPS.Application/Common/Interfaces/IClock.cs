namespace QPS.Application.Common.Interfaces;

/// <summary>
/// The university's local time (Africa/Cairo by default). Quiz status, "today" and
/// greetings all use this so the server's own time zone never matters.
/// </summary>
public interface IClock
{
    DateTime Now { get; }
    DateOnly Today { get; }
}
