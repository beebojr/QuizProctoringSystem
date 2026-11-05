using Microsoft.EntityFrameworkCore;
using QPS.Application.Common.Exceptions;
using QPS.Application.Common.Interfaces;
using QPS.Domain.Entities;

namespace QPS.Application.Common.Services;

public static class DbContextExtensions
{
    /// <summary>Loads the single settings row, creating it with defaults on first use.</summary>
    public static async Task<SystemSettings> GetSettingsAsync(this IApplicationDbContext db, CancellationToken ct)
    {
        var settings = await db.SystemSettings.OrderBy(s => s.CreatedAt).FirstOrDefaultAsync(ct);
        if (settings is not null) return settings;
        settings = new SystemSettings();
        db.SystemSettings.Add(settings);
        await db.SaveChangesAsync(ct);
        return settings;
    }

    /// <summary>The requested semester, or the active one when none is given.</summary>
    public static async Task<Semester> ResolveSemesterAsync(this IApplicationDbContext db, Guid? semesterId, CancellationToken ct)
    {
        if (semesterId.HasValue && semesterId.Value != Guid.Empty)
        {
            return await db.Semesters.FirstOrDefaultAsync(s => s.Id == semesterId.Value, ct)
                   ?? throw new NotFoundException("Semester", semesterId.Value);
        }

        return await db.Semesters.Where(s => s.IsActive).OrderByDescending(s => s.StartDate).FirstOrDefaultAsync(ct)
               ?? await db.Semesters.OrderByDescending(s => s.StartDate).FirstOrDefaultAsync(ct)
               ?? throw new NotFoundException("There is no semester yet. Create one in Settings first.");
    }
}
