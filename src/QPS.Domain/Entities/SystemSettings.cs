namespace QPS.Domain.Entities;

/// <summary>
/// Single-row table with the rule parameters. Everything is configurable because
/// requirements are expected to change.
/// </summary>
public class SystemSettings : BaseEntity
{
    /// <summary>Rule 1: the global "x" (max proctoring sessions per TA per semester).</summary>
    public int DefaultMaxSessionsPerSemester { get; set; } = 20;

    /// <summary>Rule 2: max proctoring sessions per TA per academic week.</summary>
    public int MaxSessionsPerWeek { get; set; } = 2;

    /// <summary>Soft workload goal per semester (workload points).</summary>
    public double DefaultTargetWorkload { get; set; } = 14;

    /// <summary>Proctors per room when the room is unknown.</summary>
    public int DefaultProctorsPerRoom { get; set; } = 2;

    /// <summary>Standby proctors placed in the Reserved location.</summary>
    public int ReserveProctors { get; set; } = 2;

    /// <summary>Put one TA who teaches the course in each room when possible.</summary>
    public bool PreferCourseTas { get; set; } = true;

    public string DepartmentName { get; set; } = "CS";
}
