using MediatR;
using QPS.Application.Common.Interfaces;
using QPS.Application.Common.Models;
using QPS.Application.Features.Auth.DTOs;

namespace QPS.Application.Features.Auth.Queries;

public record GetCurrentUserQuery : IRequest<Result<CurrentUserDto>>;

public class GetCurrentUserHandler : IRequestHandler<GetCurrentUserQuery, Result<CurrentUserDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;

    public GetCurrentUserHandler(IApplicationDbContext context, ICurrentUserService currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public Task<Result<CurrentUserDto>> Handle(
        GetCurrentUserQuery request, CancellationToken cancellationToken)
    {
        if (!_currentUser.UserId.HasValue)
            return Task.FromResult(Result<CurrentUserDto>.Fail("Not authenticated"));

        var user = _context.Users.FirstOrDefault(u => u.Id == _currentUser.UserId.Value);
        if (user == null)
            return Task.FromResult(Result<CurrentUserDto>.Fail("User not found"));

        var dto = new CurrentUserDto(
            user.Id,
            user.Email,
            user.FirstName,
            user.LastName,
            user.FullName,
            user.Role.ToString(),
            user.IsActive);

        return Task.FromResult(Result<CurrentUserDto>.Ok(dto));
    }
}