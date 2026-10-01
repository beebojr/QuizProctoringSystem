using MediatR;
using Microsoft.AspNetCore.Http;
using QPS.Application.Common.Models;
using QPS.Domain.Enums;

namespace QPS.Application.Features.Quizzes.DTOs;

public record QuizDto(
    Guid Id,
    string CourseName,
    DateOnly QuizDate,
    TimeOnly StartTime,
    TimeOnly EndTime,
    int WeekNumber,
    int SlotNumber,
    string Group,
    QuizStatus Status,
    bool AutoAssign,
    bool AddBackup,
    List<QuizLocationDto> Locations);

public record QuizLocationDto(
    Guid Id,
    string RoomName,
    int ProctorsNeeded,
    List<ProctorAssignmentDto> Assignments);

public record ProctorAssignmentDto(
    Guid Id,
    string TAName,
    bool IsBackup,
    AssignmentStatus Status);

public record CreateQuizCommand(
    Guid CourseId,
    Guid SemesterId,
    DateOnly QuizDate,
    TimeOnly StartTime,
    TimeOnly EndTime,
    int WeekNumber,
    int SlotNumber,
    string Group,
    bool AutoAssign,
    bool AddBackup,
    List<CreateQuizLocationDto> Locations) : IRequest<Result<QuizDto>>;

public record CreateQuizLocationDto(
    string RoomName,
    int ProctorsNeeded);

public record ImportQuizzesCommand(
    Guid SemesterId,
    IFormFile File) : IRequest<Result<List<QuizDto>>>;