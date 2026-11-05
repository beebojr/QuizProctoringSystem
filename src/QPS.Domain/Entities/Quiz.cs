using QPS.Domain.Enums;

namespace QPS.Domain.Entities;

/// <summary>
/// One exam sitting: a course at a date and time. All groups that sit it at the same time
/// are locations of the same quiz.
/// </summary>
public class Quiz : BaseEntity
{
    public Guid CourseId { get; set; }
    public Course Course { get; set; } = null!;
    public Guid SemesterId { get; set; }
    public Semester Semester { get; set; } = null!;

    public ExamType ExamType { get; set; } = ExamType.Quiz;
    public int WeekNumber { get; set; }
    public DateOnly QuizDate { get; set; }
    public TimeOnly StartTime { get; set; }
    public TimeOnly EndTime { get; set; }

    /// <summary>1-5 for the fixed slots, 0 for the 3:15-3:45 gap, null for a custom time.</summary>
    public int? SlotNumber { get; set; }

    public string? Notes { get; set; }
    public bool AutoAssign { get; set; }

    /// <summary>"Add automatic backup": the quiz has a Reserved location with standby proctors.</summary>
    public bool HasReserve { get; set; }

    public QuizSource Source { get; set; } = QuizSource.Manual;

    public ICollection<QuizLocation> Locations { get; set; } = new List<QuizLocation>();
}
