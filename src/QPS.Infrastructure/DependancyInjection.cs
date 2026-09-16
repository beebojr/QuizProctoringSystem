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
        services.AddScoped<IProctorAssignmentService, ProctorAssignmentService>();
        return services;
    }
}
