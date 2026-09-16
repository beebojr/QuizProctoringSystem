using MediatR;
using Microsoft.EntityFrameworkCore;
using QPS.Application.Common.Interfaces;
using QPS.Application.Features.Dashboard.DTOs;
using QPS.Domain.Enums;

namespace QPS.Application.Features.Dashboard.Queries;

public record GetReportQuery(
    DateOnly FromDate,
    DateOnly ToDate,
    Guid? SemesterId = null) : IRequest<ReportDto>;

public class GetReportHandler : IRequestHandler<GetReportQuery, ReportDto>
{
    private readonly IApplicationDbContext _context;

    public GetReportHandler(IApplicationDbContext context) => _context = context;

    public async Task<ReportDto> Handle(
        GetReportQuery request, CancellationToken cancellationToken)
    {
        var query = _context.ProctorAssignments
            .Include(a => a.User)
            .Include(a => a.QuizLocation)
                .ThenInclude(ql => ql.Quiz)
            .AsNoTracking()
            .Where(a =>
                a.QuizLocation.Quiz.QuizDate >= request.FromDate &&
                a.QuizLocation.Quiz.QuizDate <= request.ToDate &&
                a.Status != AssignmentStatus.Cancelled &&
                !a.IsDeleted);

        if (request.SemesterId.HasValue)
            query = query.Where(a => a.QuizLocation.Quiz.SemesterId == request.SemesterId.Value);

        var assignments = await query.ToListAsync(cancellationToken);

        var grouped = assignments
            .GroupBy(a => a.UserId)
            .Select(g =>
            {
                var user = _context.Users.First(u => u.Id == g.Key);
                var total = g.Count();
                var completed = g.Count(a => a.QuizLocation.Quiz.Status == QuizStatus.Finished);
                var upcoming = g.Count(a => a.QuizLocation.Quiz.Status == QuizStatus.Upcoming);
                return new TAReportDto(
                    user.FullName,
                    user.Email,
                    total,
                    completed,
                    upcoming,
                    total > 0 ? Math.Round((double)completed / total * 100, 1) : 0);
            })
            .OrderByDescending(r => r.TotalSessions)
            .ToList();

        return new ReportDto(request.FromDate, request.ToDate, grouped);
    }
}