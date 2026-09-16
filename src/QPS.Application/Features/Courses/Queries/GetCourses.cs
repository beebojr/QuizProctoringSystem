using MediatR;
using Microsoft.EntityFrameworkCore;
using QPS.Application.Common.Interfaces;
using QPS.Application.Features.Courses.DTOs;

namespace QPS.Application.Features.Courses.Queries;

public record GetCoursesQuery(string? Department = null) : IRequest<List<CourseDto>>;

public class GetCoursesHandler : IRequestHandler<GetCoursesQuery, List<CourseDto>>
{
    private readonly IApplicationDbContext _context;

    public GetCoursesHandler(IApplicationDbContext context) => _context = context;

    public async Task<List<CourseDto>> Handle(
        GetCoursesQuery request, CancellationToken cancellationToken)
    {
        var query = _context.Courses.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(request.Department))
            query = query.Where(c => c.Department == request.Department);

        return await query
            .OrderBy(c => c.Name)
            .Select(c => new CourseDto(c.Id, c.Name, c.Department, c.CreatedAt))
            .ToListAsync(cancellationToken);
    }
}