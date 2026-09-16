using MediatR;
using QPS.Application.Common.Models;

namespace QPS.Application.Features.Semesters.DTOs;

public record SemesterDto(
    Guid Id,
    string Name,
    DateOnly StartDate,
    DateOnly EndDate,
    bool IsActive);

public record CreateSemesterCommand(
    string Name,
    DateOnly StartDate,
    DateOnly EndDate) : IRequest<Result<SemesterDto>>;

public record UpdateSemesterCommand(
    Guid Id,
    string Name,
    DateOnly StartDate,
    DateOnly EndDate,
    bool IsActive) : IRequest<Result<SemesterDto>>;
