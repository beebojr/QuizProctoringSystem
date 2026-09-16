using MediatR;
using QPS.Application.Common.Models;
using Enums = QPS.Domain.Enums;

namespace QPS.Application.Features.Schedules.DTOs;

public record ScheduleSlotDto(
    Guid Id,
    Guid UserId,
    string TAName,
    Enums.DayOfWeek DayOfWeek,
    int SlotNumber,
    string CourseName,
    Enums.SlotType SlotType,
    string Room,
    TimeOnly StartTime,
    TimeOnly EndTime);

public record CreateScheduleSlotCommand(
    Guid UserId,
    Guid SemesterId,
    Enums.DayOfWeek DayOfWeek,
    int SlotNumber,
    string CourseName,
    Enums.SlotType SlotType,
    string Room) : IRequest<Result<ScheduleSlotDto>>;
