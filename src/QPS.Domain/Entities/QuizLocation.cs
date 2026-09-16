namespace QPS.Domain.Entities;

public class QuizLocation : BaseEntity
{
    public Guid QuizId { get; set; }
    public Quiz Quiz { get; set; } = null!;
    public string RoomName { get; set; } = string.Empty;
    public int ProctorsNeeded { get; set; }
    
    public ICollection<ProctorAssignment> Assignments { get; set; } = new List<ProctorAssignment>();
}
