using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using QPS.Application.Common.Exceptions;
using QPS.Application.Common.Interfaces;
using QPS.Application.Common.Models;
using QPS.Application.Common.Services;
using QPS.Application.Features.Assignments.DTOs;
using QPS.Application.Features.Quizzes.DTOs;
using QPS.Domain.Entities;
using QPS.Domain.Enums;
using QPS.Domain.Rules;

namespace QPS.Application.Features.Assignments.Commands;

/// <summary>Manual assignment. Rule violations block it unless Force is set with a reason.</summary>
public record AssignProctorCommand(Guid LocationId, Guid UserId, bool Force = false, string? OverrideReason = null)
    : IRequest<AssignOutcomeDto>;

public record RemoveAssignmentCommand(Guid Id) : IRequest<Result>;

/// <summary>Removes the proctor and fills the seat with the best eligible TA.</summary>
public record ReplaceAssignmentCommand(Guid Id) : IRequest<ReplaceOutcomeDto>;

public record AutoAssignQuizCommand(Guid QuizId) : IRequest<Result<AutoAssignSummaryDto>>;

/// <summary>Dry run of the batch assignment over upcoming quizzes; nothing is saved.</summary>
public record AutoAssignPreviewCommand(Guid? SemesterId, DateOnly? From = null, DateOnly? To = null, List<Guid>? QuizIds = null)
    : IRequest<BatchPreviewDto>;

/// <summary>Saves the reviewed proposals; every item is re-checked against the rules first.</summary>
public record AutoAssignCommitCommand(Guid? SemesterId, List<BatchCommitItem> Items) : IRequest<Result<BatchCommitResultDto>>;

public record ClearQuizAssignmentsCommand(Guid QuizId) : IRequest<Result>;

public class AssignProctorValidator : AbstractValidator<AssignProctorCommand>
{
    public AssignProctorValidator()
    {
        RuleFor(x => x.LocationId).NotEmpty();
        RuleFor(x => x.UserId).NotEmpty();
        RuleFor(x => x.OverrideReason).NotEmpty().When(x => x.Force)
            .WithMessage("Say why you are overriding the rules.").MaximumLength(300);
    }
}

public class AssignmentCommandHandlers :
    IRequestHandler<AssignProctorCommand, AssignOutcomeDto>,
    IRequestHandler<RemoveAssignmentCommand, Result>,
    IRequestHandler<ReplaceAssignmentCommand, ReplaceOutcomeDto>,
    IRequestHandler<AutoAssignQuizCommand, Result<AutoAssignSummaryDto>>,
    IRequestHandler<AutoAssignPreviewCommand, BatchPreviewDto>,
    IRequestHandler<AutoAssignCommitCommand, Result<BatchCommitResultDto>>,
    IRequestHandler<ClearQuizAssignmentsCommand, Result>
{
    private readonly IApplicationDbContext _db;
    private readonly IClock _clock;
    private readonly AssignmentWorkspace _workspace;
    private readonly AutoAssigner _autoAssigner;
    private readonly INotificationService _notifications;

    public AssignmentCommandHandlers(IApplicationDbContext db, IClock clock, AssignmentWorkspace workspace,
        AutoAssigner autoAssigner, INotificationService notifications)
    {
        _db = db;
        _clock = clock;
        _workspace = workspace;
        _autoAssigner = autoAssigner;
        _notifications = notifications;
    }

    public async Task<AssignOutcomeDto> Handle(AssignProctorCommand request, CancellationToken ct)
    {
        var location = await LoadLocationAsync(request.LocationId, ct);
        var quiz = location.Quiz;
        var context = await _workspace.LoadAsync(quiz.SemesterId, ct);

        if (!context.Profiles.TryGetValue(request.UserId, out var profile))
            return new AssignOutcomeDto(false, null, new List<RuleViolationDto>(), "Only TAs can proctor.");
        if (location.Assignments.Any(a => a.UserId == request.UserId))
            return new AssignOutcomeDto(false, null, new List<RuleViolationDto>(), $"{profile.Name} is already in {location.RoomName}.");
        if (location.Assignments.Count >= location.ProctorsNeeded)
            return new AssignOutcomeDto(false, null, new List<RuleViolationDto>(),
                $"{location.RoomName} already has all {location.ProctorsNeeded} proctors. Raise the number first.");

        var session = context.SessionFor(quiz);
        var violations = context.Evaluate(request.UserId, session);
        if (violations.Any(v => v.Code == RuleCode.Inactive))
            return new AssignOutcomeDto(false, null, Map(violations), $"{profile.Name}'s account is deactivated. Activate it first.");
        if (violations.Any(v => v.Code == RuleCode.AlreadyInQuiz))
            return new AssignOutcomeDto(false, null, Map(violations), $"{profile.Name} is already proctoring this quiz in another room.");
        if (violations.Count > 0 && !request.Force)
            return new AssignOutcomeDto(false, null, Map(violations), $"{profile.Name} can't take this seat.");

        var assignment = new ProctorAssignment
        {
            QuizLocationId = location.Id,
            UserId = request.UserId,
            IsCourseTa = profile.CourseIds.Contains(quiz.CourseId),
            Source = AssignmentSource.Manual,
            OverrideReason = violations.Count > 0 ? request.OverrideReason!.Trim() : null,
            Points = 1
        };
        _db.ProctorAssignments.Add(assignment);
        if (quiz.QuizDate.ToDateTime(quiz.StartTime) > _clock.Now)
            AssignmentNotifier.NotifyAssigned(_notifications, new[]
            {
                new AssignmentNotice(request.UserId, quiz.Course.Name, quiz.QuizDate, quiz.StartTime, quiz.EndTime, location.RoomName, location.IsReserve)
            });
        await _db.SaveChangesAsync(ct);

        return new AssignOutcomeDto(true, assignment.Id, Map(violations),
            violations.Count > 0 ? $"{profile.Name} assigned with an override." : $"{profile.Name} assigned to {location.RoomName}.");
    }

    public async Task<Result> Handle(RemoveAssignmentCommand request, CancellationToken ct)
    {
        var assignment = await LoadAssignmentAsync(request.Id, ct);
        var quiz = assignment.QuizLocation.Quiz;
        _db.ProctorAssignments.Remove(assignment);
        if (quiz.QuizDate.ToDateTime(quiz.StartTime) > _clock.Now)
            _notifications.Notify(assignment.UserId,
                $"You are no longer proctoring {quiz.Course.Name} on {Format.Date(quiz.QuizDate)} ({assignment.QuizLocation.RoomName}).",
                NotificationType.Assignment, "/my-assignments");
        await _db.SaveChangesAsync(ct);
        return Result.Ok($"{assignment.User.FullName} removed from {assignment.QuizLocation.RoomName}.");
    }

    public async Task<ReplaceOutcomeDto> Handle(ReplaceAssignmentCommand request, CancellationToken ct)
    {
        var assignment = await LoadAssignmentAsync(request.Id, ct);
        var location = assignment.QuizLocation;
        var quiz = location.Quiz;
        if (quiz.QuizDate.ToDateTime(quiz.StartTime) <= _clock.Now)
            return new ReplaceOutcomeDto(false, null, "This quiz has already started.");

        var context = await _workspace.LoadAsync(quiz.SemesterId, ct);
        context.Unbook(assignment.UserId, location.Id);
        var session = context.SessionFor(quiz);

        var others = await _db.ProctorAssignments.AsNoTracking()
            .Where(a => a.QuizLocationId == location.Id && a.Id != assignment.Id)
            .ToListAsync(ct);
        var eligible = context.ActivePool
            .Where(p => p.Id != assignment.UserId && context.Evaluate(p.Id, session).Count == 0)
            .ToList();

        ProctorProfile? pick = null;
        if (context.Settings.PreferCourseTas && !others.Any(o => o.IsCourseTa))
            pick = AssignmentEngine.Rank(eligible.Where(p => p.CourseIds.Contains(quiz.CourseId)), session, context.Ledger).FirstOrDefault();
        pick ??= AssignmentEngine.Rank(eligible, session, context.Ledger).FirstOrDefault();

        if (pick is null)
            return new ReplaceOutcomeDto(false, null, "No eligible TA is free for this quiz. The current proctor was kept.");

        _db.ProctorAssignments.Remove(assignment);
        _db.ProctorAssignments.Add(new ProctorAssignment
        {
            QuizLocationId = location.Id,
            UserId = pick.Id,
            IsCourseTa = pick.CourseIds.Contains(quiz.CourseId),
            Source = AssignmentSource.Auto,
            Points = 1
        });
        _notifications.Notify(assignment.UserId,
            $"You are no longer proctoring {quiz.Course.Name} on {Format.Date(quiz.QuizDate)}; {pick.Name} replaces you.",
            NotificationType.Assignment, "/my-assignments");
        AssignmentNotifier.NotifyAssigned(_notifications, new[]
        {
            new AssignmentNotice(pick.Id, quiz.Course.Name, quiz.QuizDate, quiz.StartTime, quiz.EndTime, location.RoomName, location.IsReserve)
        });
        await _db.SaveChangesAsync(ct);
        return new ReplaceOutcomeDto(true, pick.Name, $"{assignment.User.FullName} replaced by {pick.Name}.");
    }

    public async Task<Result<AutoAssignSummaryDto>> Handle(AutoAssignQuizCommand request, CancellationToken ct)
    {
        var quiz = await _db.Quizzes.AsNoTracking().FirstOrDefaultAsync(q => q.Id == request.QuizId, ct)
                   ?? throw new NotFoundException("Quiz", request.QuizId);
        if (quiz.QuizDate.ToDateTime(quiz.StartTime) <= _clock.Now)
            return Result<AutoAssignSummaryDto>.Fail("This quiz has already started, so it can't be auto-assigned.");

        var run = await _autoAssigner.PlanAsync(quiz.SemesterId, q => q.Where(x => x.Id == quiz.Id), ct);
        var summary = await _autoAssigner.ApplyAsync(run, ct);
        var message = summary.Assigned == 0 && summary.Missing == 0 ? "Already fully staffed."
            : summary.Missing == 0 ? $"Assigned {summary.Assigned} proctor(s); fully staffed."
            : $"Assigned {summary.Assigned} proctor(s); {summary.Missing} seat(s) could not be filled.";
        return Result<AutoAssignSummaryDto>.Ok(summary, message);
    }

    public async Task<BatchPreviewDto> Handle(AutoAssignPreviewCommand request, CancellationToken ct)
    {
        var semester = await _db.ResolveSemesterAsync(request.SemesterId, ct);
        var ids = request.QuizIds?.ToHashSet();
        var run = await _autoAssigner.PlanAsync(semester.Id, q =>
        {
            if (request.From.HasValue) q = q.Where(x => x.QuizDate >= request.From.Value);
            if (request.To.HasValue) q = q.Where(x => x.QuizDate <= request.To.Value);
            if (ids is { Count: > 0 }) q = q.Where(x => ids.Contains(x.Id));
            return q;
        }, ct);

        var quizzes = run.Quizzes.ToDictionary(q => q.Id);
        var locations = run.Quizzes.SelectMany(q => q.Locations).ToDictionary(l => l.Id);
        var items = run.Plan.Assignments.Select(p =>
        {
            var q = quizzes[p.QuizId];
            var l = locations[p.LocationId];
            return new BatchItemDto(q.Id, l.Id, p.UserId, q.Course.Name, q.QuizDate, q.StartTime, q.EndTime,
                q.WeekNumber, l.RoomName, l.IsReserve, run.Context.Profiles[p.UserId].Name, p.IsCourseTa);
        }).ToList();

        var emptySeats = run.Quizzes.SelectMany(q => q.Locations).Sum(l => Math.Max(0, l.ProctorsNeeded - l.Assignments.Count));
        var shortfalls = run.Plan.Shortfalls.Select(s => Quizzes.QuizMapper.ToDto(s, quizzes[s.QuizId])).ToList();
        return new BatchPreviewDto(run.Quizzes.Count(q => q.Locations.Any(l => l.Assignments.Count < l.ProctorsNeeded)), emptySeats, items, shortfalls);
    }

    public async Task<Result<BatchCommitResultDto>> Handle(AutoAssignCommitCommand request, CancellationToken ct)
    {
        if (request.Items.Count == 0) return Result<BatchCommitResultDto>.Fail("Nothing to save.");
        var semester = await _db.ResolveSemesterAsync(request.SemesterId, ct);
        var context = await _workspace.LoadAsync(semester.Id, ct);
        var locationIds = request.Items.Select(i => i.LocationId).Distinct().ToList();
        var locations = await _db.QuizLocations
            .Include(l => l.Assignments)
            .Include(l => l.Quiz).ThenInclude(q => q.Course)
            .Where(l => locationIds.Contains(l.Id) && l.Quiz.SemesterId == semester.Id)
            .ToDictionaryAsync(l => l.Id, ct);

        var rejected = new List<string>();
        var notices = new List<AssignmentNotice>();
        // Seats taken before this commit; new assignments are counted separately because EF
        // also adds them to location.Assignments as soon as they are tracked.
        var existingCounts = locations.ToDictionary(l => l.Key, l => l.Value.Assignments.Count);
        var added = new Dictionary<Guid, int>();
        var now = _clock.Now;

        var ordered = request.Items
            .Where(i => locations.ContainsKey(i.LocationId))
            .OrderBy(i => locations[i.LocationId].Quiz.QuizDate)
            .ThenBy(i => locations[i.LocationId].Quiz.StartTime);

        foreach (var item in ordered)
        {
            var location = locations[item.LocationId];
            var quiz = location.Quiz;
            var name = context.Profiles.TryGetValue(item.UserId, out var p) ? p.Name : "Unknown TA";
            var label = $"{name} → {quiz.Course.Name} {Format.Date(quiz.QuizDate)} {location.RoomName}";

            if (quiz.QuizDate.ToDateTime(quiz.StartTime) <= now) { rejected.Add($"{label}: the quiz has started."); continue; }
            if (existingCounts[location.Id] + added.GetValueOrDefault(location.Id) >= location.ProctorsNeeded)
            { rejected.Add($"{label}: the room is already full."); continue; }

            var session = context.SessionFor(quiz);
            var violations = context.Evaluate(item.UserId, session);
            if (violations.Count > 0) { rejected.Add($"{label}: {violations[0].Message}"); continue; }

            _db.ProctorAssignments.Add(new ProctorAssignment
            {
                QuizLocationId = location.Id,
                UserId = item.UserId,
                IsCourseTa = p!.CourseIds.Contains(quiz.CourseId),
                Source = AssignmentSource.Auto,
                Points = 1
            });
            context.Book(item.UserId, session, location.Id);
            added[location.Id] = added.GetValueOrDefault(location.Id) + 1;
            notices.Add(new AssignmentNotice(item.UserId, quiz.Course.Name, quiz.QuizDate, quiz.StartTime, quiz.EndTime, location.RoomName, location.IsReserve));
        }

        foreach (var missing in request.Items.Where(i => !locations.ContainsKey(i.LocationId)))
            rejected.Add("A room in the proposal no longer exists.");

        AssignmentNotifier.NotifyAssigned(_notifications, notices);
        await _db.SaveChangesAsync(ct);
        return Result<BatchCommitResultDto>.Ok(new BatchCommitResultDto(notices.Count, rejected),
            rejected.Count == 0 ? $"Saved {notices.Count} assignment(s)." : $"Saved {notices.Count} assignment(s); {rejected.Count} skipped.");
    }

    public async Task<Result> Handle(ClearQuizAssignmentsCommand request, CancellationToken ct)
    {
        var quiz = await _db.Quizzes
            .Include(q => q.Course)
            .Include(q => q.Locations).ThenInclude(l => l.Assignments)
            .FirstOrDefaultAsync(q => q.Id == request.QuizId, ct)
            ?? throw new NotFoundException("Quiz", request.QuizId);
        if (quiz.QuizDate.ToDateTime(quiz.StartTime) <= _clock.Now)
            return Result.Fail("This quiz has already started; its proctors are kept as history.");

        var all = quiz.Locations.SelectMany(l => l.Assignments).ToList();
        foreach (var userId in all.Select(a => a.UserId).Distinct())
            _notifications.Notify(userId, $"You are no longer proctoring {quiz.Course.Name} on {Format.Date(quiz.QuizDate)}.",
                NotificationType.Assignment, "/my-assignments");
        _db.ProctorAssignments.RemoveRange(all);
        await _db.SaveChangesAsync(ct);
        return Result.Ok($"Removed {all.Count} proctor(s).");
    }

    private async Task<QuizLocation> LoadLocationAsync(Guid id, CancellationToken ct) =>
        await _db.QuizLocations
            .Include(l => l.Assignments)
            .Include(l => l.Quiz).ThenInclude(q => q.Course)
            .FirstOrDefaultAsync(l => l.Id == id, ct)
        ?? throw new NotFoundException("Location", id);

    private async Task<ProctorAssignment> LoadAssignmentAsync(Guid id, CancellationToken ct) =>
        await _db.ProctorAssignments
            .Include(a => a.User)
            .Include(a => a.QuizLocation).ThenInclude(l => l.Quiz).ThenInclude(q => q.Course)
            .FirstOrDefaultAsync(a => a.Id == id, ct)
        ?? throw new NotFoundException("Assignment", id);

    private static List<RuleViolationDto> Map(IEnumerable<RuleViolation> violations) =>
        violations.Select(v => new RuleViolationDto(v.Code.ToString(), v.Message)).ToList();
}
