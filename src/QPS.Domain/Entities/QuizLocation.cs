namespace QPS.Domain.Entities;

public class QuizLocation : BaseEntity
{
    public Guid QuizId { get; set; }
    public Quiz Quiz { get; set; } = null!;
    public string RoomName { get; set; } = string.Empty;

    /// <summary>Student group sitting in this room (e.g. GPI, GPII, GPIII).</summary>
    public string? Group { get; set; }

    public int ProctorsNeeded { get; set; }

    /// <summary>True for the "Reserved" backup location whose proctors are on standby.</summary>
    public bool IsReserve { get; set; }

    public int SortOrder { get; set; }

    public ICollection<ProctorAssignment> Assignments { get; set; } = new List<ProctorAssignment>();
}
