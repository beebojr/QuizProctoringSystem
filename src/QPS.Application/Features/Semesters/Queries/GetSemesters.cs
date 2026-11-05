using MediatR;
using Microsoft.EntityFrameworkCore;
using QPS.Application.Common.Interfaces;
using QPS.Application.Features.Semesters.DTOs;
using QPS.Domain.Entities;
using QPS.Domain.Scheduling;

namespace QPS.Application.Features.Semesters.Queries;

public record GetSemestersQuery : IRequest<List<SemesterDto>>;

public class GetSemestersHandler : IRequestHandler<GetSemestersQuery, List<SemesterDto>>
{
    private readonly IApplicationDbContext _db;
    private readonly IClock _clock;

    public GetSemestersHandler(IApplicationDbContext db, IClock clock)
    {
        _db = db;
        _clock = clock;
    }

    public async Task<List<SemesterDto>> Handle(GetSemestersQuery request, CancellationToken ct)
    {
        var semesters = await _db.Semesters.AsNoTracking().OrderByDescending(s => s.StartDate).ToListAsync(ct);
        return semesters.Select(s => ToDto(s, _clock.Today)).ToList();
    }

    public static SemesterDto ToDto(Semester s, DateOnly today)
    {
        var total = AcademicCalendar.WeekNumber(s.StartDate, s.EndDate);
        var current = Math.Clamp(AcademicCalendar.WeekNumber(s.StartDate, today), 0, Math.Max(total, 1) + 1);
        return new SemesterDto(s.Id, s.Name, s.StartDate, s.EndDate, s.IsActive, current, total);
    }
}
