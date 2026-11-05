namespace QPS.Domain.Entities;

public class Semester : BaseEntity
{
    public string Name { get; set; } = string.Empty;

    /// <summary>First day of academic Week 1. Every week is the 7 days from this weekday.</summary>
    public DateOnly StartDate { get; set; }

    public DateOnly EndDate { get; set; }

    /// <summary>Only one semester is active at a time.</summary>
    public bool IsActive { get; set; }
}
