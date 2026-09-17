# Task 14: Notifications for the Quiz Proctoring System

## Status: DONE

## Commits created
- be3e3ba Add notification infrastructure (send, read, mark-as-read)

## Build status
- Build succeeded (dotnet build "Quiz Proctoring System.sln")
- 0 Warning(s), 0 Error(s)

## Files created
1. `src/QPS.Application/Features/Notifications/DTOs/NotificationDtos.cs`
2. `src/QPS.Application/Features/Notifications/Queries/GetNotifications.cs`
3. `src/QPS.Application/Features/Notifications/Commands/MarkAsRead.cs`
4. `src/QPS.Application/Features/Notifications/Commands/MarkAllAsRead.cs`
5. `src/QPS.API/Controllers/NotificationsController.cs`

## Summary
Created notification infrastructure with:
- NotificationDto record for data transfer
- GetNotificationsQuery for retrieving notifications with optional filtering by UserId and IsRead
- MarkAsReadCommand for marking individual notifications as read
- MarkAllAsReadCommand for marking all unread notifications as read
- NotificationsController with endpoints for GET, PUT mark-as-read, and PUT mark-all-as-read
- All code follows existing patterns and conventions
- Solution builds successfully with no warnings or errors