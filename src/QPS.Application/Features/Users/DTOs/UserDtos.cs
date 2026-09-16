using MediatR;
using QPS.Application.Common.Models;
using QPS.Domain.Enums;
using DayOfWeek = QPS.Domain.Enums.DayOfWeek;

namespace QPS.Application.Features.Users.DTOs;

public record UserDto(
    Guid Id,
    string Email,
    string FirstName,
    string LastName,
    string FullName,
    UserRole Role,
    DayOfWeek? DayOff,
    int TargetWorkload,
    int MaxProctoringSessionsPerSemester,
    bool IsActive,
    int AssignmentCount);

public record CreateUserCommand(
    string Email,
    string Password,
    string FirstName,
    string LastName,
    UserRole Role,
    DayOfWeek? DayOff = null,
    int TargetWorkload = 14,
    int MaxProctoringSessionsPerSemester = 10) : IRequest<Result<UserDto>>;
