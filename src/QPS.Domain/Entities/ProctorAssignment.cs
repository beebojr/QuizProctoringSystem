using QPS.Domain.Enums;

namespace QPS.Domain.Entities;

public class ProctorAssignment : BaseEntity
{
    public Guid QuizLocationId { get; set; }
    public QuizLocation QuizLocation { get; set; } = null!;
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;

    /// <summary>The TA teaches this course (shown with a flag next to the name).</summary>
    public bool IsCourseTa { get; set; }

    public AssignmentSource Source { get; set; }

    /// <summary>Set when an admin assigned someone despite a rule violation.</summary>
    public string? OverrideReason { get; set; }

    /// <summary>Workload points earned by this assignment (1 per session).</summary>
    public double Points { get; set; } = 1;
}
