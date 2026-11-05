using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using QPS.Application.Common.Exceptions;
using QPS.Application.Common.Interfaces;
using QPS.Application.Common.Models;
using QPS.Application.Common.Services;
using QPS.Domain.Entities;
using QPS.Domain.Enums;

namespace QPS.Application.Features.Excuses;

public record ExcuseDto(
    Guid Id,
    Guid UserId,
    string TaName,
    string Reason,
    DateOnly StartDate,
    DateOnly EndDate,
    bool SubmittedByTa,
    bool IsActive,
    DateTime CreatedAt);

public record AffectedAssignmentDto(Guid AssignmentId, Guid QuizId, string CourseName, DateOnly Date, TimeOnly StartTime, TimeOnly EndTime, string RoomName);

public record ExcuseCreatedDto(ExcuseDto Excuse, List<AffectedAssignmentDto> AffectedAssignments);

public record GetExcusesQuery(Guid? UserId = null, bool ActiveOnly = false) : IRequest<List<ExcuseDto>>;

/// <summary>TAs submit their own excuse; admins can add one for any TA.</summary>
public record CreateExcuseCommand(Guid? UserId, string Reason, DateOnly StartDate, DateOnly EndDate) : IRequest<Result<ExcuseCreatedDto>>;

public record DeleteExcuseCommand(Guid Id) : IRequest<Result>;

public class CreateExcuseValidator : AbstractValidator<CreateExcuseCommand>
{
    public CreateExcuseValidator()
    {
        RuleFor(x => x.Reason).NotEmpty().WithMessage("Give a reason.").MaximumLength(500);
        RuleFor(x => x.EndDate).GreaterThanOrEqualTo(x => x.StartDate).WithMessage("The end date can't be before the start date.");
        RuleFor(x => x).Must(x => x.EndDate.DayNumber - x.StartDate.DayNumber <= 180)
            .WithMessage("An excuse can cover at most 180 days.");
    }
}

public class ExcuseHandlers :
    IRequestHandler<GetExcusesQuery, List<ExcuseDto>>,
    IRequestHandler<CreateExcuseCommand, Result<ExcuseCreatedDto>>,
    IRequestHandler<DeleteExcuseCommand, Result>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly IClock _clock;
    private readonly INotificationService _notifications;

    public ExcuseHandlers(IApplicationDbContext db, ICurrentUserService currentUser, IClock clock, INotificationService notifications)
    {
        _db = db;
        _currentUser = currentUser;
        _clock = clock;
        _notifications = notifications;
    }

    public async Task<List<ExcuseDto>> Handle(GetExcusesQuery request, CancellationToken ct)
    {
        var userId = _currentUser.IsAdmin ? request.UserId : _currentUser.RequireUserId();
        var query = _db.Excuses.AsNoTracking().Include(e => e.User).Where(e => !e.User.IsDeleted);
        if (userId.HasValue) query = query.Where(e => e.UserId == userId.Value);
        var today = _clock.Today;
        if (request.ActiveOnly) query = query.Where(e => e.EndDate >= today);

        var list = await query.ToListAsync(ct);
        return list
            .OrderByDescending(e => e.EndDate >= today).ThenBy(e => e.StartDate)
            .Select(e => ToDto(e, today))
            .ToList();
    }

    public async Task<Result<ExcuseCreatedDto>> Handle(CreateExcuseCommand request, CancellationToken ct)
    {
        var currentId = _currentUser.RequireUserId();
        var targetId = request.UserId ?? currentId;
        if (!_currentUser.IsAdmin && targetId != currentId)
            throw new ForbiddenAccessException("You can only submit excuses for yourself.");

        var ta = await _db.Users.FirstOrDefaultAsync(u => u.Id == targetId && !u.IsDeleted, ct)
                 ?? throw new NotFoundException("TA", targetId);
        if (ta.Role != UserRole.TA) return Result<ExcuseCreatedDto>.Fail("Excuses are only for TAs.");

        var excuse = new Excuse
        {
            UserId = ta.Id,
            Reason = request.Reason.Trim(),
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            SubmittedByTa = !_currentUser.IsAdmin,
            CreatedById = currentId
        };
        _db.Excuses.Add(excuse);

        var affected = await _db.ProctorAssignments.AsNoTracking()
            .Where(a => a.UserId == ta.Id
                        && a.QuizLocation.Quiz.QuizDate >= request.StartDate
                        && a.QuizLocation.Quiz.QuizDate <= request.EndDate
                        && a.QuizLocation.Quiz.QuizDate >= _clock.Today)
            .Select(a => new AffectedAssignmentDto(a.Id, a.QuizLocation.QuizId, a.QuizLocation.Quiz.Course.Name,
                a.QuizLocation.Quiz.QuizDate, a.QuizLocation.Quiz.StartTime, a.QuizLocation.Quiz.EndTime, a.QuizLocation.RoomName))
            .ToListAsync(ct);

        var period = $"{Format.Date(request.StartDate)} – {Format.Date(request.EndDate)}";
        var impact = affected.Count == 0 ? "" : $" {affected.Count} upcoming proctoring session{(affected.Count == 1 ? " needs" : "s need")} a replacement.";
        if (excuse.SubmittedByTa)
        {
            await _notifications.NotifyAdminsAsync($"TA {ta.FullName} submitted an excuse for {period}: {excuse.Reason}.{impact}",
                NotificationType.Excuse, $"/proctors?ta={ta.Id}", ct);
        }
        else
        {
            _notifications.Notify(ta.Id, $"An administrator recorded an excuse for you ({period}). You won't be assigned to proctor in that period.",
                NotificationType.Excuse, "/my-excuses");
            if (affected.Count > 0)
                await _notifications.NotifyAdminsAsync($"{ta.FullName} is excused for {period}.{impact}",
                    NotificationType.Excuse, $"/proctors?ta={ta.Id}", ct);
        }

        await _db.SaveChangesAsync(ct);
        excuse.User = ta;
        return Result<ExcuseCreatedDto>.Ok(new ExcuseCreatedDto(ToDto(excuse, _clock.Today), affected),
            affected.Count == 0 ? "Excuse saved." : $"Excuse saved.{impact}");
    }

    public async Task<Result> Handle(DeleteExcuseCommand request, CancellationToken ct)
    {
        var excuse = await _db.Excuses.Include(e => e.User).FirstOrDefaultAsync(e => e.Id == request.Id, ct)
                     ?? throw new NotFoundException("Excuse", request.Id);
        if (!_currentUser.IsAdmin && excuse.UserId != _currentUser.RequireUserId())
            throw new ForbiddenAccessException("You can only withdraw your own excuses.");

        _db.Excuses.Remove(excuse);
        if (!_currentUser.IsAdmin)
            await _notifications.NotifyAdminsAsync(
                $"TA {excuse.User.FullName} withdrew the excuse for {Format.Date(excuse.StartDate)} – {Format.Date(excuse.EndDate)}.",
                NotificationType.Excuse, $"/proctors?ta={excuse.UserId}", ct);
        else
            _notifications.Notify(excuse.UserId,
                $"Your excuse for {Format.Date(excuse.StartDate)} – {Format.Date(excuse.EndDate)} was removed by an administrator.",
                NotificationType.Excuse, "/my-excuses");
        await _db.SaveChangesAsync(ct);
        return Result.Ok("Excuse removed.");
    }

    public static ExcuseDto ToDto(Excuse e, DateOnly today) => new(
        e.Id, e.UserId, e.User?.FullName ?? string.Empty, e.Reason, e.StartDate, e.EndDate,
        e.SubmittedByTa, e.EndDate >= today, e.CreatedAt);
}
