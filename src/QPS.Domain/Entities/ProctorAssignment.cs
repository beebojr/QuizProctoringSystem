using QPS.Domain.Enums;

namespace QPS.Domain.Entities;

public class ProctorAssignment : BaseEntity
{
    public Guid QuizLocationId { get; set; }
    public QuizLocation QuizLocation { get; set; } = null!;
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;
    public bool IsBackup { get; set; }
    public AssignmentStatus Status { get; set; }
}
