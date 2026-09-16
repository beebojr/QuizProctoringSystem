using MediatR;
using QPS.Application.Common.Models;
using QPS.Domain.Entities;
using QPS.Domain.Enums;

namespace QPS.Application.Features.Assignments.DTOs;

public record AssignmentDto(
    Guid Id,
    string QuizCourseName,
    DateOnly QuizDate,
    TimeOnly QuizTime,
    string RoomName,
    string TAName,
    string TAEmail,
    bool IsBackup,
    AssignmentStatus Status);

public record ManualAssignCommand(
    Guid QuizLocationId,
    Guid UserId,
    bool IsBackup = false) : IRequest<Result<ProctorAssignment>>;