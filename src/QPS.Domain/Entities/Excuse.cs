namespace QPS.Domain.Entities;

public class Excuse : BaseEntity
{
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;
    public string Reason { get; set; } = string.Empty;
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public bool IsActive { get; set; }
}
