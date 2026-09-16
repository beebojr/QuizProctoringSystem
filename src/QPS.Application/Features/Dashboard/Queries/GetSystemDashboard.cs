using MediatR;
using Microsoft.EntityFrameworkCore;
using QPS.Application.Common.Interfaces;
using QPS.Application.Features.Dashboard.DTOs;
using QPS.Domain.Enums;

namespace QPS.Application.Features.Dashboard.Queries;

public record GetSystemDashboardQuery : IRequest<SystemDashboardDto>;

public class GetSystemDashboardHandler : IRequestHandler<GetSystemDashboardQuery, SystemDashboardDto>
{
    private readonly IApplicationDbContext _context;

    public GetSystemDashboardHandler(IApplicationDbContext context) => _context = context;

    public async Task<SystemDashboardDto> Handle(
        GetSystemDashboardQuery request, CancellationToken cancellationToken)
    {
        return new SystemDashboardDto(
            await _context.Users.CountAsync(u => u.Role == UserRole.TA, cancellationToken),
            await _context.Users.CountAsync(u => u.Role == UserRole.TA && u.IsActive, cancellationToken),
            await _context.Quizzes.CountAsync(cancellationToken),
            await _context.Quizzes.CountAsync(q => q.Status == QuizStatus.Upcoming, cancellationToken),
            await _context.ProctorAssignments.CountAsync(a => !a.IsDeleted, cancellationToken),
            await _context.Quizzes.CountAsync(q =>
                q.Status == QuizStatus.Upcoming &&
                !q.Locations.Any(l => l.Assignments.Any(a => !a.IsBackup && !a.IsDeleted)), cancellationToken));
    }
}