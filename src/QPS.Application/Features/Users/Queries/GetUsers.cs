using MediatR;
using Microsoft.EntityFrameworkCore;
using QPS.Application.Common.Interfaces;
using QPS.Application.Features.Users.DTOs;
using QPS.Domain.Enums;

namespace QPS.Application.Features.Users.Queries;

public record GetUsersQuery(
    UserRole? Role = null,
    string? Search = null) : IRequest<List<UserDto>>;

public class GetUsersHandler : IRequestHandler<GetUsersQuery, List<UserDto>>
{
    private readonly IApplicationDbContext _context;

    public GetUsersHandler(IApplicationDbContext context) => _context = context;

    public async Task<List<UserDto>> Handle(
        GetUsersQuery request, CancellationToken cancellationToken)
    {
        var users = await _context.Users
            .AsNoTracking()
            .AsQueryable()
            .Where(u => !u.IsDeleted)
            .ToListAsync(cancellationToken);

        if (request.Role.HasValue)
            users = users.Where(u => u.Role == request.Role.Value).ToList();

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var term = request.Search.ToLower();
            users = users.Where(u =>
                u.Email.ToLower().Contains(term) ||
                u.FirstName.ToLower().Contains(term) ||
                u.LastName.ToLower().Contains(term)).ToList();
        }

        var userIds = users.Select(u => u.Id).ToList();
        var assignmentCounts = await _context.ProctorAssignments
            .AsNoTracking()
            .Where(a => userIds.Contains(a.UserId) && a.Status != AssignmentStatus.Cancelled && !a.IsDeleted)
            .GroupBy(a => a.UserId)
            .Select(g => new { UserId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(g => g.UserId, g => g.Count, cancellationToken);

        return users
            .Select(u => new UserDto(
                u.Id, u.Email, u.FirstName, u.LastName, u.FullName,
                u.Role, u.DayOff, u.TargetWorkload,
                u.MaxProctoringSessionsPerSemester, u.IsActive,
                assignmentCounts.GetValueOrDefault(u.Id, 0)))
            .OrderBy(u => u.LastName)
            .ThenBy(u => u.FirstName)
            .ToList();
    }
}
