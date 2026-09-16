using MediatR;
using Microsoft.EntityFrameworkCore;
using QPS.Application.Common.Interfaces;
using QPS.Application.Features.Quizzes.DTOs;
using QPS.Domain.Enums;

namespace QPS.Application.Features.Quizzes.Queries;

public record GetQuizzesQuery(
    Guid? SemesterId = null,
    QuizStatus? Status = null) : IRequest<List<QuizDto>>;

public class GetQuizzesHandler : IRequestHandler<GetQuizzesQuery, List<QuizDto>>
{
    private readonly IApplicationDbContext _context;

    public GetQuizzesHandler(IApplicationDbContext context) => _context = context;

    public async Task<List<QuizDto>> Handle(
        GetQuizzesQuery request, CancellationToken cancellationToken)
    {
        var query = _context.Quizzes
            .Include(q => q.Course)
            .Include(q => q.Locations)
                .ThenInclude(l => l.Assignments)
                    .ThenInclude(a => a.User)
            .AsNoTracking()
            .AsQueryable();

        if (request.SemesterId.HasValue)
            query = query.Where(q => q.SemesterId == request.SemesterId.Value);

        if (request.Status.HasValue)
            query = query.Where(q => q.Status == request.Status.Value);

        return await query
            .OrderBy(q => q.QuizDate)
            .ThenBy(q => q.StartTime)
            .Select(q => new QuizDto(
                q.Id,
                q.Course.Name,
                q.QuizDate,
                q.StartTime,
                q.EndTime,
                q.Status,
                q.AutoAssign,
                q.AddBackup,
                q.Locations.Select(l => new QuizLocationDto(
                    l.Id,
                    l.RoomName,
                    l.ProctorsNeeded,
                    l.Assignments.Select(a => new ProctorAssignmentDto(
                        a.Id,
                        a.User.FirstName + " " + a.User.LastName,
                        a.IsBackup,
                        a.Status)).ToList())).ToList()))
            .ToListAsync(cancellationToken);
    }
}