using QPS.Domain.Enums;

namespace QPS.Domain.Entities;

public class TAScheduleSlot : BaseEntity
{
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;
    public Guid SemesterId { get; set; }
    public Semester Semester { get; set; } = null!;
    public Enums.DayOfWeek DayOfWeek { get; set; }
    public int SlotNumber { get; set; }
    public string CourseName { get; set; } = string.Empty;
    public SlotType SlotType { get; set; }
    public string Room { get; set; } = string.Empty;
    public TimeOnly StartTime { get; set; }
    public TimeOnly EndTime { get; set; }
}
