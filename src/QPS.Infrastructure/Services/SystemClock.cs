using Microsoft.Extensions.Configuration;
using QPS.Application.Common.Interfaces;

namespace QPS.Infrastructure.Services;

/// <summary>Current time in the university's time zone (App:TimeZone, default Africa/Cairo).</summary>
public class SystemClock : IClock
{
    private readonly TimeZoneInfo _zone;

    public SystemClock(IConfiguration configuration)
    {
        _zone = Resolve(configuration["App:TimeZone"] ?? "Africa/Cairo");
    }

    public DateTime Now => TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, _zone);

    public DateOnly Today => DateOnly.FromDateTime(Now);

    private static TimeZoneInfo Resolve(string id)
    {
        foreach (var candidate in new[] { id, "Egypt Standard Time" })
        {
            try
            {
                return TimeZoneInfo.FindSystemTimeZoneById(candidate);
            }
            catch (TimeZoneNotFoundException)
            {
            }
            catch (InvalidTimeZoneException)
            {
            }
        }
        return TimeZoneInfo.Local;
    }
}
