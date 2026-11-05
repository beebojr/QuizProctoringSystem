using QPS.Application.Common.Interfaces;
using QPS.Domain.Enums;

namespace QPS.Application.Common.Services;

public sealed record AssignmentNotice(Guid UserId, string CourseName, DateOnly Date, TimeOnly Start, TimeOnly End, string RoomName, bool IsReserve);

public static class AssignmentNotifier
{
    /// <summary>One notification per assignment, or a single summary when a TA gets many at once.</summary>
    public static void NotifyAssigned(INotificationService notifications, IEnumerable<AssignmentNotice> notices)
    {
        foreach (var group in notices.GroupBy(n => n.UserId))
        {
            var list = group.OrderBy(n => n.Date).ThenBy(n => n.Start).ToList();
            if (list.Count <= 3)
            {
                foreach (var n in list)
                {
                    var where = n.IsReserve ? "as a reserve proctor" : $"in {n.RoomName}";
                    notifications.Notify(n.UserId,
                        $"You were assigned to proctor {n.CourseName} on {Format.Date(n.Date)}, {Format.TimeRange(n.Start, n.End)} {where}.",
                        NotificationType.Assignment, "/my-assignments");
                }
            }
            else
            {
                notifications.Notify(group.Key,
                    $"You have {list.Count} new proctoring assignments, starting {Format.Date(list[0].Date)}.",
                    NotificationType.Assignment, "/my-assignments");
            }
        }
    }
}
