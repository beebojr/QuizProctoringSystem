namespace QPS.Domain.Entities;

public class Course : BaseEntity
{
    public string Name { get; set; } = string.Empty;

    /// <summary>Lower-case, single-spaced name used for matching imported calendars.</summary>
    public string NormalizedName { get; set; } = string.Empty;

    public string? Code { get; set; }
    public string Department { get; set; } = "CS";
}
