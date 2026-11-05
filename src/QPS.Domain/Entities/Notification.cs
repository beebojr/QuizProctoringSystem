using QPS.Domain.Enums;

namespace QPS.Domain.Entities;

public class Notification : BaseEntity
{
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;
    public string Message { get; set; } = string.Empty;
    public NotificationType Type { get; set; }

    /// <summary>Optional in-app route to open (e.g. "/quizzes?focus=...").</summary>
    public string? Link { get; set; }

    public bool IsRead { get; set; }
}
