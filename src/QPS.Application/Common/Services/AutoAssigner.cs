using Microsoft.EntityFrameworkCore;
using QPS.Application.Common.Interfaces;
using QPS.Application.Features.Quizzes;
using QPS.Application.Features.Quizzes.DTOs;
using QPS.Domain.Entities;
using QPS.Domain.Enums;
using QPS.Domain.Rules;

namespace QPS.Application.Common.Services;

public sealed record PlannedRun(AssignmentContext Context, List<Quiz> Quizzes, AssignmentPlan Plan);

/// <summary>Runs the batch assignment engine over a set of upcoming quizzes and saves the result.</summary>
public sealed class AutoAssigner
{
    private readonly IApplicationDbContext _db;
    private readonly AssignmentWorkspace _workspace;
    private readonly INotificationService _notifications;
    private readonly IClock _clock;

    public AutoAssigner(IApplicationDbContext db, AssignmentWorkspace workspace, INotificationService notifications, IClock clock)
    {
        _db = db;
        _workspace = workspace;
        _notifications = notifications;
        _clock = clock;
    }

    /// <summary>Plans assignments for the upcoming quizzes selected by <paramref name="filter"/>.</summary>
    public async Task<PlannedRun> PlanAsync(Guid semesterId, Func<IQueryable<Quiz>, IQueryable<Quiz>> filter, CancellationToken ct)
    {
        var context = await _workspace.LoadAsync(semesterId, ct);
        var quizzes = await filter(_workspace.QuizzesWithStaffing().Where(q => q.SemesterId == semesterId))
            .ToListAsync(ct);

        var now = _clock.Now;
        var upcoming = quizzes.Where(q => q.QuizDate.ToDateTime(q.StartTime) > now).ToList();
        var demands = AssignmentWorkspace.BuildDemands(context, upcoming);
        var options = new EngineOptions(context.Rules, context.Settings.PreferCourseTas);
        var plan = AssignmentEngine.Plan(demands, context.ActivePool, context.Ledger, options);
        return new PlannedRun(context, upcoming, plan);
    }

    /// <summary>Saves a plan produced by <see cref="PlanAsync"/> and notifies the TAs.</summary>
    public async Task<AutoAssignSummaryDto> ApplyAsync(PlannedRun run, CancellationToken ct)
    {
        var quizzesById = run.Quizzes.ToDictionary(q => q.Id);
        var locationsById = run.Quizzes.SelectMany(q => q.Locations).ToDictionary(l => l.Id);
        var notices = new List<AssignmentNotice>();

        foreach (var p in run.Plan.Assignments)
        {
            var location = locationsById[p.LocationId];
            var quiz = quizzesById[p.QuizId];
            var assignment = new ProctorAssignment
            {
                QuizLocationId = p.LocationId,
                UserId = p.UserId,
                IsCourseTa = p.IsCourseTa,
                Source = AssignmentSource.Auto,
                Points = 1
            };
            _db.ProctorAssignments.Add(assignment);
            notices.Add(new AssignmentNotice(p.UserId, quiz.Course.Name, quiz.QuizDate, quiz.StartTime, quiz.EndTime,
                location.RoomName, location.IsReserve));
        }

        AssignmentNotifier.NotifyAssigned(_notifications, notices);
        await _db.SaveChangesAsync(ct);

        var shortfalls = run.Plan.Shortfalls.Select(s => QuizMapper.ToDto(s, quizzesById[s.QuizId])).ToList();
        return new AutoAssignSummaryDto(run.Plan.Assignments.Count, shortfalls.Sum(s => s.Missing), shortfalls);
    }
}
