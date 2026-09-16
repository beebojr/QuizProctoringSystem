using MediatR;
using QPS.Application.Common.Interfaces;
using QPS.Application.Common.Models;
using QPS.Application.Features.Assignments.DTOs;
using QPS.Domain.Entities;

namespace QPS.Application.Features.Assignments.Commands;

public record AutoAssignCommand(Guid QuizId) : IRequest<Result<List<AssignmentDto>>>;

public class AutoAssignHandler : IRequestHandler<AutoAssignCommand, Result<List<AssignmentDto>>>
{
    private readonly IProctorAssignmentService _assignmentService;
    private readonly IApplicationDbContext _context;

    public AutoAssignHandler(
        IProctorAssignmentService assignmentService,
        IApplicationDbContext context)
    {
        _assignmentService = assignmentService;
        _context = context;
    }

    public async Task<Result<List<AssignmentDto>>> Handle(
        AutoAssignCommand request, CancellationToken cancellationToken)
    {
        var assignments = await _assignmentService.AutoAssignAsync(
            request.QuizId, cancellationToken);

        var dtos = assignments.Select(a => new AssignmentDto(
            a.Id,
            _context.Quizzes.First(q => q.Locations.Any(l => l.Id == a.QuizLocationId)).Course.Name,
            _context.Quizzes.First(q => q.Locations.Any(l => l.Id == a.QuizLocationId)).QuizDate,
            _context.Quizzes.First(q => q.Locations.Any(l => l.Id == a.QuizLocationId)).StartTime,
            _context.QuizLocations.First(l => l.Id == a.QuizLocationId).RoomName,
            _context.Users.First(u => u.Id == a.UserId).FullName,
            _context.Users.First(u => u.Id == a.UserId).Email,
            a.IsBackup,
            a.Status)).ToList();

        return Result<List<AssignmentDto>>.Ok(dtos, $"Assigned {assignments.Count} proctors");
    }
}