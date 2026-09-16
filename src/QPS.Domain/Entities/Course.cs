namespace QPS.Domain.Entities;

public class Course : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Department { get; set; } = string.Empty;
}
