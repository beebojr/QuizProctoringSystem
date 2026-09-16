using MediatR;
using Microsoft.EntityFrameworkCore;
using QPS.Application.Common.Interfaces;
using QPS.Application.Features.Semesters.DTOs;

namespace QPS.Application.Features.Semesters.Queries;

public record GetSemestersQuery : IRequest<List<SemesterDto>>;

public class GetSemestersHandler : IRequestHandler<GetSemestersQuery, List<SemesterDto>>
{
    private readonly IApplicationDbContext _context;

    public GetSemestersHandler(IApplicationDbContext context) => _context = context;

    public async Task<List<SemesterDto>> Handle(
        GetSemestersQuery request, CancellationToken cancellationToken)
    {
        return await _context.Semesters
            .AsNoTracking()
            .OrderByDescending(s => s.StartDate)
            .Select(s => new SemesterDto(s.Id, s.Name, s.StartDate, s.EndDate, s.IsActive))
            .ToListAsync(cancellationToken);
    }
}
