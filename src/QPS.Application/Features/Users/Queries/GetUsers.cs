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
        var query = _context.Users
            .AsNoTracking()
            .AsQueryable();

        if (request.Role.HasValue)
            query = query.Where(u => u.Role == request.Role.Value);

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var term = request.Search.ToLower();
            query = query.Where(u =>
                u.Email.ToLower().Contains(term) ||
                u.FirstName.ToLower().Contains(term) ||
                u.LastName.ToLower().Contains(term));
        }

        return await query
            .Select(u => new UserDto(
                u.Id, u.Email, u.FirstName, u.LastName, u.FullName,
                u.Role, u.DayOff, u.TargetWorkload,
                u.MaxProctoringSessionsPerSemester, u.IsActive,
                _context.ProctorAssignments.Count(a => a.UserId == u.Id && a.Status != AssignmentStatus.Cancelled && !a.IsDeleted)))
            .OrderBy(u => u.LastName)
            .ThenBy(u => u.FirstName)
            .ToListAsync(cancellationToken);
    }
}
