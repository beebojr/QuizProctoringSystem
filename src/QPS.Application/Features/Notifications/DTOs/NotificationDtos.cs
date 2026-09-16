namespace QPS.Application.Features.Notifications.DTOs;

public record NotificationDto(
    Guid Id,
    string Message,
    bool IsRead,
    DateTime CreatedAt);