# Task 5: Infrastructure Services

## Context

This is the Infrastructure layer for a Quiz Proctoring System following Clean Architecture. It implements Application layer interfaces and provides JWT authentication services.

## Requirements

1. Delete the default `Class1.cs` file in QPS.Infrastructure
2. Create the Services directory
3. Create TokenService.cs
4. Create CurrentUserService.cs
5. Create NotificationService.cs
6. Create DependancyInjection.cs
7. Verify the project builds: `dotnet build src/QPS.Infrastructure/QPS.Infrastructure.csproj`
8. Commit your work

## Files to Create

### 1. TokenService

`src/QPS.Infrastructure/Services/TokenService.cs`:
```csharp
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using QPS.Domain.Entities;

namespace QPS.Infrastructure.Services;

public interface ITokenService
{
    string GenerateAccessToken(User user);
    string GenerateRefreshToken();
    ClaimsPrincipal? ValidateToken(string token);
}

public class TokenService : ITokenService
{
    private readonly IConfiguration _configuration;

    public TokenService(IConfiguration configuration)
        => _configuration = configuration;

    public string GenerateAccessToken(User user)
    {
        var key = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(_configuration["Jwt:Secret"]!));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim(ClaimTypes.Name, user.FullName),
            new Claim(ClaimTypes.Role, user.Role.ToString())
        };

        var token = new JwtSecurityToken(
            issuer: _configuration["Jwt:Issuer"],
            audience: _configuration["Jwt:Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(
                double.Parse(_configuration["Jwt:AccessTokenExpirationMinutes"] ?? "60")),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public string GenerateRefreshToken()
    {
        var randomNumber = new byte[64];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomNumber);
        return Convert.ToBase64String(randomNumber);
    }

    public ClaimsPrincipal? ValidateToken(string token)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.UTF8.GetBytes(_configuration["Jwt:Secret"]!);

        try
        {
            return tokenHandler.ValidateToken(token, new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(key),
                ValidateIssuer = true,
                ValidIssuer = _configuration["Jwt:Issuer"],
                ValidateAudience = true,
                ValidAudience = _configuration["Jwt:Audience"],
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero
            }, out _);
        }
        catch
        {
            return null;
        }
    }
}
```

### 2. CurrentUserService

`src/QPS.Infrastructure/Services/CurrentUserService.cs`:
```csharp
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using QPS.Application.Common.Interfaces;

namespace QPS.Infrastructure.Services;

public class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUserService(IHttpContextAccessor httpContextAccessor)
        => _httpContextAccessor = httpContextAccessor;

    public Guid? UserId
    {
        get
        {
            var userId = _httpContextAccessor.HttpContext?.User?
                .FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return Guid.TryParse(userId, out var id) ? id : null;
        }
    }

    public bool IsAuthenticated =>
        _httpContextAccessor.HttpContext?.User?.Identity?.IsAuthenticated ?? false;
}
```

### 3. NotificationService

`src/QPS.Infrastructure/Services/NotificationService.cs`:
```csharp
using QPS.Application.Common.Interfaces;
using QPS.Data;
using QPS.Domain.Entities;

namespace QPS.Infrastructure.Services;

public class NotificationService : INotificationService
{
    private readonly AppDbContext _context;

    public NotificationService(AppDbContext context)
        => _context = context;

    public async Task NotifyAsync(Guid userId, string message, CancellationToken ct = default)
    {
        var notification = new Notification
        {
            UserId = userId,
            Message = message,
            IsRead = false
        };

        _context.Notifications.Add(notification);
        await _context.SaveChangesAsync(ct);
    }

    public async Task NotifyManyAsync(List<Guid> userIds, string message, CancellationToken ct = default)
    {
        var notifications = userIds.Select(userId => new Notification
        {
            UserId = userId,
            Message = message,
            IsRead = false
        }).ToList();

        _context.Notifications.AddRange(notifications);
        await _context.SaveChangesAsync(ct);
    }
}
```

### 4. DependencyInjection

`src/QPS.Infrastructure/DependancyInjection.cs`:
```csharp
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using QPS.Application.Common.Interfaces;
using QPS.Infrastructure.Services;

namespace QPS.Infrastructure;

public static class DependancyInjection
{
    public static IServiceCollection AddInfrastructureServices(
        this IServiceCollection services)
    {
        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUserService, CurrentUserService>();
        services.AddScoped<ITokenService, TokenService>();
        services.AddScoped<INotificationService, NotificationService>();
        return services;
    }
}
```

## Interfaces to Implement

- `ICurrentUserService` (from QPS.Application.Common.Interfaces)
- `INotificationService` (from QPS.Application.Common.Interfaces)
- `ITokenService` (defined in TokenService.cs)

## Dependencies

- `AppDbContext` (from QPS.Data)
- `User` entity (from QPS.Domain.Entities)
- `Notification` entity (from QPS.Domain.Entities)
- `IConfiguration` (Microsoft.Extensions.Configuration)
- `IHttpContextAccessor` (Microsoft.AspNetCore.Http)

## Global Constraints

- .NET 8 SDK required
- Follow Clean Architecture dependency rules
- All entities inherit from BaseEntity (Id, CreatedAt, UpdatedAt, soft delete)
- JWT Bearer authentication
- BCrypt for password hashing (not used in this task)

## Report File

Write your report to: `C:\Users\Saif Saad\Documents\Default Project\.superpowers\sdd\2026-09-16-quiz-proctoring-system\task-5-report.md`

Return status: DONE | BLOCKED | NEEDS_CONTEXT
Include: commits created (short SHA + subject), build status (pass/fail), report file path.