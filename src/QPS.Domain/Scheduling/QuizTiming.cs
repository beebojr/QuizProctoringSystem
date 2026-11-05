using QPS.Domain.Enums;

namespace QPS.Domain.Scheduling;

public static class QuizTiming
{
    /// <summary>Upcoming before the start, Started during the quiz, Finished afterwards.</summary>
    public static QuizStatus StatusAt(DateOnly date, TimeOnly start, TimeOnly end, DateTime localNow)
    {
        var startAt = date.ToDateTime(start);
        var endAt = date.ToDateTime(end);
        if (localNow < startAt) return QuizStatus.Upcoming;
        if (localNow < endAt) return QuizStatus.Started;
        return QuizStatus.Finished;
    }

    public static bool Overlaps(TimeOnly aStart, TimeOnly aEnd, TimeOnly bStart, TimeOnly bEnd) =>
        aStart < bEnd && aEnd > bStart;
}
