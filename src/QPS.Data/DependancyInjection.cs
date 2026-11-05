using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using QPS.Application.Common.Interfaces;

namespace QPS.Data;

public static class DependancyInjection
{
    public static IServiceCollection AddDataServices(
        this IServiceCollection services, IConfiguration configuration)
    {
        var connection = configuration.GetConnectionString("DefaultConnection") ?? "Data Source=quizproctoring.db";
        services.AddDbContext<AppDbContext>(options => Configure(options, connection));

        services.AddScoped<IApplicationDbContext>(provider =>
            provider.GetRequiredService<AppDbContext>());

        return services;
    }

    public static void Configure(DbContextOptionsBuilder options, string connectionString) =>
        options.UseSqlite(connectionString, sqlite =>
            sqlite.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery));
}
