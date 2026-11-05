using MediatR;
using Microsoft.EntityFrameworkCore;
using QPS.Application.Common.Exceptions;
using QPS.Application.Common.Interfaces;
using QPS.Application.Common.Services;
using QPS.Application.Features.Quizzes.DTOs;
using QPS.Domain.Enums;

namespace QPS.Application.Features.Quizzes.Queries;

public record GetQuizzesQuery(
    Guid? SemesterId = null,
    QuizStatus? Status = null,
    string? Search = null,
    bool IncompleteOnly = false,
    DateOnly? From = null,
    DateOnly? To = null,
    Guid? CourseId = null) : IRequest<List<QuizDto>>;

public record GetQuizQuery(Guid Id) : IRequest<QuizDto>;

public class GetQuizzesHandler :
    IRequestHandler<GetQuizzesQuery, List<QuizDto>>,
    IRequestHandler<GetQuizQuery, QuizDto>
{
    private readonly IApplicationDbContext _db;
    private readonly IClock _clock;
    private readonly AssignmentWorkspace _workspace;

    public GetQuizzesHandler(IApplicationDbContext db, IClock clock, AssignmentWorkspace workspace)
    {
        _db = db;
        _clock = clock;
        _workspace = workspace;
    }

    public async Task<List<QuizDto>> Handle(GetQuizzesQuery request, CancellationToken ct)
    {
        var semester = await _db.ResolveSemesterAsync(request.SemesterId, ct);
        var query = _db.Quizzes.AsNoTracking()
            .Include(q => q.Course)
            .Include(q => q.Locations).ThenInclude(l => l.Assignments).ThenInclude(a => a.User)
            .Where(q => q.SemesterId == semester.Id);

        if (request.From.HasValue) query = query.Where(q => q.QuizDate >= request.From.Value);
        if (request.To.HasValue) query = query.Where(q => q.QuizDate <= request.To.Value);
        if (request.CourseId.HasValue) query = query.Where(q => q.CourseId == request.CourseId.Value);

        var quizzes = await query.ToListAsync(ct);
        var context = await _workspace.LoadAsync(semester.Id, ct);
        var now = _clock.Now;

        var dtos = quizzes.Select(q => QuizMapper.ToDto(q, now, context));

        if (request.Status.HasValue) dtos = dtos.Where(d => d.Status == request.Status.Value);
        if (request.IncompleteOnly) dtos = dtos.Where(d => !d.IsFullyStaffed || d.WarningCount > 0);
        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var term = request.Search.Trim();
            dtos = dtos.Where(d => Matches(d, term));
        }

        return dtos.OrderBy(d => d.QuizDate).ThenBy(d => d.StartTime).ThenBy(d => d.CourseName).ToList();
    }

    public async Task<QuizDto> Handle(GetQuizQuery request, CancellationToken ct)
    {
        var quiz = await _db.Quizzes.AsNoTracking()
            .Include(q => q.Course)
            .Include(q => q.Locations).ThenInclude(l => l.Assignments).ThenInclude(a => a.User)
            .FirstOrDefaultAsync(q => q.Id == request.Id, ct)
            ?? throw new NotFoundException("Quiz", request.Id);
        var context = await _workspace.LoadAsync(quiz.SemesterId, ct);
        return QuizMapper.ToDto(quiz, _clock.Now, context);
    }

    private static bool Matches(QuizDto d, string term)
    {
        bool Has(string? s) => s is not null && s.Contains(term, StringComparison.OrdinalIgnoreCase);
        return Has(d.CourseName)
               || Has($"week {d.WeekNumber}")
               || Has(d.SlotLabel)
               || d.Locations.Any(l => Has(l.RoomName) || Has(l.Group) || l.Proctors.Any(p => Has(p.Name) || Has(p.Email)));
    }
}
