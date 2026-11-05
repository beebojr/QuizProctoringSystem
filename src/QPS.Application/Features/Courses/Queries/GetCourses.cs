using MediatR;
using Microsoft.EntityFrameworkCore;
using QPS.Application.Common.Interfaces;
using QPS.Application.Features.Courses.DTOs;

namespace QPS.Application.Features.Courses.Queries;

public record GetCoursesQuery(string? Department = null, string? Search = null) : IRequest<List<CourseDto>>;

public class GetCoursesHandler : IRequestHandler<GetCoursesQuery, List<CourseDto>>
{
    private readonly IApplicationDbContext _db;

    public GetCoursesHandler(IApplicationDbContext db) => _db = db;

    public async Task<List<CourseDto>> Handle(GetCoursesQuery request, CancellationToken ct)
    {
        var query = _db.Courses.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(request.Department))
            query = query.Where(c => c.Department == request.Department);

        var courses = await query.ToListAsync(ct);
        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var term = request.Search.Trim();
            courses = courses.Where(c => c.Name.Contains(term, StringComparison.OrdinalIgnoreCase)
                                         || (c.Code ?? "").Contains(term, StringComparison.OrdinalIgnoreCase)).ToList();
        }

        var quizCounts = await _db.Quizzes.AsNoTracking()
            .GroupBy(q => q.CourseId).Select(g => new { g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.Key, x => x.Count, ct);
        var taCounts = await _db.TAScheduleSlots.AsNoTracking()
            .Where(s => s.CourseId != null)
            .Select(s => new { s.CourseId, s.UserId }).Distinct()
            .GroupBy(x => x.CourseId!.Value).Select(g => new { g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.Key, x => x.Count, ct);

        return courses
            .OrderBy(c => c.Name, StringComparer.OrdinalIgnoreCase)
            .Select(c => new CourseDto(c.Id, c.Name, c.Code, c.Department,
                quizCounts.GetValueOrDefault(c.Id), taCounts.GetValueOrDefault(c.Id)))
            .ToList();
    }
}
