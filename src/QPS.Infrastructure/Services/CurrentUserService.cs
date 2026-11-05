using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using QPS.Application.Common.Interfaces;

namespace QPS.Infrastructure.Services;

public class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUserService(IHttpContextAccessor httpContextAccessor)
        => _httpContextAccessor = httpContextAccessor;

    private ClaimsPrincipal? Principal => _httpContextAccessor.HttpContext?.User;

    public Guid? UserId
    {
        get
        {
            var value = Principal?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return Guid.TryParse(value, out var id) ? id : null;
        }
    }

    public bool IsAuthenticated => Principal?.Identity?.IsAuthenticated ?? false;

    public bool IsAdmin => Principal?.IsInRole("Admin") ?? false;

    public Guid RequireUserId() =>
        UserId ?? throw new UnauthorizedAccessException("Sign in to continue.");
}
