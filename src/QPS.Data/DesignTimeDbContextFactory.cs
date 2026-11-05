using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace QPS.Data;

/// <summary>Lets "dotnet ef migrations add" work without starting the API.</summary>
public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>();
        DependancyInjection.Configure(options, "Data Source=quizproctoring.db");
        return new AppDbContext(options.Options);
    }
}
