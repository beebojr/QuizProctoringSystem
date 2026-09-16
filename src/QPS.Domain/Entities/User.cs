using QPS.Domain.Enums;

namespace QPS.Domain.Entities;

public class User : BaseEntity
{
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public UserRole Role { get; set; }
    public Enums.DayOfWeek? DayOff { get; set; }
    public int TargetWorkload { get; set; } = 14;
    public int MaxProctoringSessionsPerSemester { get; set; } = 10;
    public bool IsActive { get; set; } = true;
    
    public string FullName => $"{FirstName} {LastName}";
}
