using MediatR;
using Microsoft.EntityFrameworkCore;
using QPS.Application.Common.Interfaces;
using QPS.Application.Features.Schedules.DTOs;

namespace QPS.Application.Features.Schedules.Queries;

public record GetSchedulesQuery(
    Guid? UserId = null,
    Guid? SemesterId = null) : IRequest<List<ScheduleSlotDto>>;

public class GetSchedulesHandler : IRequestHandler<GetSchedulesQuery, List<ScheduleSlotDto>>
{
    private readonly IApplicationDbContext _context;

    public GetSchedulesHandler(IApplicationDbContext context) => _context = context;

    public async Task<List<ScheduleSlotDto>> Handle(
        GetSchedulesQuery request, CancellationToken cancellationToken)
    {
        var query = _context.TAScheduleSlots
            .Include(s => s.User)
            .AsNoTracking()
            .AsQueryable();

        if (request.UserId.HasValue)
            query = query.Where(s => s.UserId == request.UserId.Value);

        if (request.SemesterId.HasValue)
            query = query.Where(s => s.SemesterId == request.SemesterId.Value);

        return await query
            .OrderBy(s => s.User.FirstName)
            .ThenBy(s => s.DayOfWeek)
            .ThenBy(s => s.SlotNumber)
            .Select(s => new ScheduleSlotDto(
                s.Id, s.UserId,
                s.User.FirstName + " " + s.User.LastName,
                s.DayOfWeek, s.SlotNumber, s.CourseName, s.SlotType,
                s.Room, s.StartTime, s.EndTime))
            .ToListAsync(cancellationToken);
    }
}
