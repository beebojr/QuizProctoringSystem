namespace QPS.Application.Features.Auth.DTOs;

public record CurrentUserDto(
    Guid Id,
    string Email,
    string FirstName,
    string LastName,
    string FullName,
    string Role,
    string Department,
    bool CanEditSchedule,
    List<string> DaysOff);

public record AuthResponseDto(
    string AccessToken,
    DateTime ExpiresAt,
    CurrentUserDto User,
    string TokenType = "Bearer");
