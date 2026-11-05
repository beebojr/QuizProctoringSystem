namespace QPS.Domain.Entities;

/// <summary>
/// An exam hall. The number of proctors a room needs depends on its size, so each room
/// remembers its default proctor count and every imported quiz uses it.
/// </summary>
public class Room : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string NormalizedName { get; set; } = string.Empty;
    public int? Capacity { get; set; }
    public int DefaultProctors { get; set; } = 2;
}
