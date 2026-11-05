using MediatR;
using Microsoft.EntityFrameworkCore;
using QPS.Application.Common.Interfaces;
using QPS.Application.Common.Services;
using QPS.Application.Features.Schedules.DTOs;
using QPS.Domain.Entities;
using QPS.Domain.Enums;
using QPS.Domain.Scheduling;

namespace QPS.Application.Features.Schedules.Queries;

public record GetSchedulesQuery(Guid? UserId = null, Guid? SemesterId = null) : IRequest<List<ScheduleSlotDto>>;

public record GetScheduleOverviewQuery(Guid? SemesterId = null) : IRequest<List<TaScheduleSummaryDto>>;

public class GetSchedulesHandler :
    IRequestHandler<GetSchedulesQuery, List<ScheduleSlotDto>>,
    IRequestHandler<GetScheduleOverviewQuery, List<TaScheduleSummaryDto>>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public GetSchedulesHandler(IApplicationDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<List<ScheduleSlotDto>> Handle(GetSchedulesQuery request, CancellationToken ct)
    {
        var semester = await _db.ResolveSemesterAsync(request.SemesterId, ct);
        // TAs can only see their own schedule.
        var userId = _currentUser.IsAdmin ? request.UserId : _currentUser.RequireUserId();

        var query = _db.TAScheduleSlots.AsNoTracking().Include(s => s.User)
            .Where(s => s.SemesterId == semester.Id && !s.User.IsDeleted);
        if (userId.HasValue) query = query.Where(s => s.UserId == userId.Value);

        var slots = await query.ToListAsync(ct);
        return slots
            .OrderBy(s => s.User.FirstName).ThenBy(s => s.User.LastName)
            .ThenBy(s => SortDay(s.DayOfWeek)).ThenBy(s => s.SlotNumber)
            .Select(ToDto)
            .ToList();
    }

    public async Task<List<TaScheduleSummaryDto>> Handle(GetScheduleOverviewQuery request, CancellationToken ct)
    {
        var semester = await _db.ResolveSemesterAsync(request.SemesterId, ct);
        var tas = await _db.Users.AsNoTracking().Where(u => u.Role == UserRole.TA && !u.IsDeleted).ToListAsync(ct);
        var counts = await _db.TAScheduleSlots.AsNoTracking()
            .Where(s => s.SemesterId == semester.Id)
            .GroupBy(s => s.UserId).Select(g => new { g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.Key, x => x.Count, ct);

        return tas
            .OrderBy(u => u.FirstName, StringComparer.OrdinalIgnoreCase).ThenBy(u => u.LastName, StringComparer.OrdinalIgnoreCase)
            .Select(u => new TaScheduleSummaryDto(u.Id, u.FullName, u.Email, Format.Days(u.DaysOff),
                counts.GetValueOrDefault(u.Id), u.CanEditSchedule, u.IsActive))
            .ToList();
    }

    public static int SortDay(QPS.Domain.Enums.DayOfWeek day) => day == QPS.Domain.Enums.DayOfWeek.Friday ? 7 : (int)day;

    public static ScheduleSlotDto ToDto(TAScheduleSlot s) => new(
        s.Id, s.UserId, s.User?.FullName ?? string.Empty, s.DayOfWeek, s.SlotNumber,
        SlotCatalog.Describe(s.SlotNumber), s.CourseId, s.CourseName, s.SlotType, s.Room, s.StartTime, s.EndTime);
}
