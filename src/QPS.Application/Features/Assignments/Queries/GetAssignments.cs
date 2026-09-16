using MediatR;
using Microsoft.EntityFrameworkCore;
using QPS.Application.Common.Interfaces;
using QPS.Application.Features.Assignments.DTOs;

namespace QPS.Application.Features.Assignments.Queries;

public record GetAssignmentsQuery(
    Guid? QuizId = null,
    Guid? UserId = null) : IRequest<List<AssignmentDto>>;

public class GetAssignmentsHandler : IRequestHandler<GetAssignmentsQuery, List<AssignmentDto>>
{
    private readonly IApplicationDbContext _context;

    public GetAssignmentsHandler(IApplicationDbContext context) => _context = context;

    public async Task<List<AssignmentDto>> Handle(
        GetAssignmentsQuery request, CancellationToken cancellationToken)
    {
        var query = _context.ProctorAssignments
            .Include(a => a.QuizLocation)
                .ThenInclude(ql => ql.Quiz)
                    .ThenInclude(q => q.Course)
            .Include(a => a.User)
            .AsNoTracking()
            .AsQueryable();

        if (request.QuizId.HasValue)
            query = query.Where(a => a.QuizLocation.QuizId == request.QuizId.Value);

        if (request.UserId.HasValue)
            query = query.Where(a => a.UserId == request.UserId.Value);

        return await query
            .Select(a => new AssignmentDto(
                a.Id,
                a.QuizLocation.Quiz.Course.Name,
                a.QuizLocation.Quiz.QuizDate,
                a.QuizLocation.Quiz.StartTime,
                a.QuizLocation.RoomName,
                a.User.FirstName + " " + a.User.LastName,
                a.User.Email,
                a.IsBackup,
                a.Status))
            .ToListAsync(cancellationToken);
    }
}