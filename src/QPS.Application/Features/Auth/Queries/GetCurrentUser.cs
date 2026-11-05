using MediatR;
using Microsoft.EntityFrameworkCore;
using QPS.Application.Common.Exceptions;
using QPS.Application.Common.Interfaces;
using QPS.Application.Common.Services;
using QPS.Application.Features.Auth.DTOs;
using QPS.Domain.Entities;

namespace QPS.Application.Features.Auth.Queries;

public record GetCurrentUserQuery : IRequest<CurrentUserDto>;

public class GetCurrentUserHandler : IRequestHandler<GetCurrentUserQuery, CurrentUserDto>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public GetCurrentUserHandler(IApplicationDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<CurrentUserDto> Handle(GetCurrentUserQuery request, CancellationToken ct)
    {
        var userId = _currentUser.RequireUserId();
        var user = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId && !u.IsDeleted, ct)
                   ?? throw new NotFoundException("Your account no longer exists.");
        if (!user.IsActive) throw new ForbiddenAccessException("This account is deactivated.");
        return ToDto(user);
    }

    public static CurrentUserDto ToDto(User user) => new(
        user.Id, user.Email, user.FirstName, user.LastName, user.FullName,
        user.Role.ToString(), user.Department, user.CanEditSchedule, Format.Days(user.DaysOff));
}
