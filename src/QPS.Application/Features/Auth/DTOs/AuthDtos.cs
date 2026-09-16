using MediatR;
using QPS.Application.Common.Models;

namespace QPS.Application.Features.Auth.DTOs;

public record RegisterCommand(
    string Email,
    string Password,
    string FirstName,
    string LastName) : IRequest<Result<AuthResponseDto>>;

public record LoginCommand(
    string Email,
    string Password) : IRequest<Result<AuthResponseDto>>;

public record AuthResponseDto(
    string AccessToken,
    string RefreshToken,
    DateTime ExpiresAt,
    string TokenType = "Bearer");

public record CurrentUserDto(
    Guid Id,
    string Email,
    string FirstName,
    string LastName,
    string FullName,
    string Role,
    bool IsActive);