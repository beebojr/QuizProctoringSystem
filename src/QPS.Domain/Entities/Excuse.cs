namespace QPS.Domain.Entities;

/// <summary>
/// A period in which a TA cannot proctor (sick, injured, travelling...).
/// The TA is never assigned to a quiz on a date inside the period.
/// </summary>
public class Excuse : BaseEntity
{
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;
    public string Reason { get; set; } = string.Empty;
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }

    /// <summary>True when the TA submitted it, false when an admin added it.</summary>
    public bool SubmittedByTa { get; set; }

    public Guid? CreatedById { get; set; }
}
