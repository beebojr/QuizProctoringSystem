using QPS.Domain.Enums;

namespace QPS.Domain.Entities;

public class User : BaseEntity
{
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public UserRole Role { get; set; }

    /// <summary>Department the user belongs to (e.g. "CS").</summary>
    public string Department { get; set; } = "CS";

    /// <summary>Extra weekly days off (Friday is always off and is not stored here).</summary>
    public WeekDays DaysOff { get; set; } = WeekDays.None;

    /// <summary>Rule 1 per-TA override. Null means "use the global default".</summary>
    public int? MaxSessionsOverride { get; set; }

    /// <summary>Soft workload goal per-TA override. Null means "use the global default".</summary>
    public double? TargetWorkloadOverride { get; set; }

    /// <summary>When true the TA may add/remove slots in their own weekly schedule.</summary>
    public bool CanEditSchedule { get; set; } = true;

    public bool IsActive { get; set; } = true;
    public DateTime? LastLoginAt { get; set; }

    public string FullName => $"{FirstName} {LastName}".Trim();
}
