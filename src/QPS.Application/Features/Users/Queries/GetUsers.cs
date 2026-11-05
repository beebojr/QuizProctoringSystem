using MediatR;
using Microsoft.EntityFrameworkCore;
using QPS.Application.Common.Exceptions;
using QPS.Application.Common.Interfaces;
using QPS.Application.Common.Services;
using QPS.Application.Features.Users.DTOs;
using QPS.Domain.Enums;

namespace QPS.Application.Features.Users.Queries;

public record GetUsersQuery(UserRole? Role = null, string? Search = null, bool IncludeInactive = true) : IRequest<List<UserDto>>;
public record GetUserQuery(Guid Id) : IRequest<UserDto>;

public class GetUsersHandler :
    IRequestHandler<GetUsersQuery, List<UserDto>>,
    IRequestHandler<GetUserQuery, UserDto>
{
    private readonly IApplicationDbContext _db;

    public GetUsersHandler(IApplicationDbContext db) => _db = db;

    public async Task<List<UserDto>> Handle(GetUsersQuery request, CancellationToken ct)
    {
        var settings = await _db.GetSettingsAsync(ct);
        var query = _db.Users.AsNoTracking().Where(u => !u.IsDeleted);
        if (request.Role.HasValue) query = query.Where(u => u.Role == request.Role.Value);
        if (!request.IncludeInactive) query = query.Where(u => u.IsActive);

        var users = await query.ToListAsync(ct);
        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var term = request.Search.Trim();
            users = users.Where(u => u.FullName.Contains(term, StringComparison.OrdinalIgnoreCase)
                                     || u.Email.Contains(term, StringComparison.OrdinalIgnoreCase)).ToList();
        }

        return users
            .OrderBy(u => u.FirstName, StringComparer.OrdinalIgnoreCase)
            .ThenBy(u => u.LastName, StringComparer.OrdinalIgnoreCase)
            .Select(u => UserDto.From(u, settings))
            .ToList();
    }

    public async Task<UserDto> Handle(GetUserQuery request, CancellationToken ct)
    {
        var settings = await _db.GetSettingsAsync(ct);
        var user = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == request.Id && !u.IsDeleted, ct)
                   ?? throw new NotFoundException("User", request.Id);
        return UserDto.From(user, settings);
    }
}
