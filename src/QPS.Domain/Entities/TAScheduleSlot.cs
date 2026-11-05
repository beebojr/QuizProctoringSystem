using QPS.Domain.Enums;

namespace QPS.Domain.Entities;

/// <summary>A teaching commitment in a TA's weekly schedule (one of the 5 fixed slots).</summary>
public class TAScheduleSlot : BaseEntity
{
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;
    public Guid SemesterId { get; set; }
    public Semester Semester { get; set; } = null!;
    public Enums.DayOfWeek DayOfWeek { get; set; }
    public int SlotNumber { get; set; }
    public Guid? CourseId { get; set; }
    public Course? Course { get; set; }
    public string CourseName { get; set; } = string.Empty;
    public SlotType SlotType { get; set; }
    public string Room { get; set; } = string.Empty;
    public TimeOnly StartTime { get; set; }
    public TimeOnly EndTime { get; set; }
}
