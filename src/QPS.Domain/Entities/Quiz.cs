using QPS.Domain.Enums;

namespace QPS.Domain.Entities;

public class Quiz : BaseEntity
{
    public Guid CourseId { get; set; }
    public Course Course { get; set; } = null!;
    public Guid SemesterId { get; set; }
    public Semester Semester { get; set; } = null!;
    public int WeekNumber { get; set; }
    public DateOnly QuizDate { get; set; }
    public TimeOnly StartTime { get; set; }
    public TimeOnly EndTime { get; set; }
    public int SlotNumber { get; set; }
    public string Group { get; set; } = string.Empty;
    public QuizStatus Status { get; set; }
    public bool AutoAssign { get; set; }
    public bool AddBackup { get; set; }
    
    public ICollection<QuizLocation> Locations { get; set; } = new List<QuizLocation>();
}
