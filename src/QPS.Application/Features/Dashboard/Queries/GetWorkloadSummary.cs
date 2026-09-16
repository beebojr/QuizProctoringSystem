using MediatR;
using Microsoft.EntityFrameworkCore;
using QPS.Application.Common.Interfaces;
using QPS.Application.Features.Dashboard.DTOs;
using QPS.Domain.Enums;

namespace QPS.Application.Features.Dashboard.Queries;

public record GetWorkloadSummaryQuery(
    Guid UserId,
    Guid SemesterId) : IRequest<WorkloadSummaryDto>;

public class GetWorkloadSummaryHandler : IRequestHandler<GetWorkloadSummaryQuery, WorkloadSummaryDto>
{
    private readonly IApplicationDbContext _context;

    public GetWorkloadSummaryHandler(IApplicationDbContext context) => _context = context;

    public async Task<WorkloadSummaryDto> Handle(
        GetWorkloadSummaryQuery request, CancellationToken cancellationToken)
    {
        var user = await _context.Users
            .FirstAsync(u => u.Id == request.UserId, cancellationToken);

        var currentSessions = await _context.ProctorAssignments
            .CountAsync(a =>
                a.UserId == request.UserId &&
                a.QuizLocation.Quiz.SemesterId == request.SemesterId &&
                a.Status != AssignmentStatus.Cancelled &&
                !a.IsDeleted, cancellationToken);

        var remaining = Math.Max(0, user.TargetWorkload - currentSessions);
        var percentage = user.TargetWorkload > 0
            ? (double)currentSessions / user.TargetWorkload * 100
            : 0;

        return new WorkloadSummaryDto(
            currentSessions,
            user.TargetWorkload,
            remaining,
            Math.Round(percentage, 1));
    }
}