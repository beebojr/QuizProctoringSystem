using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using QPS.Application.Common.Exceptions;
using QPS.Application.Common.Interfaces;
using QPS.Application.Common.Models;
using QPS.Application.Common.Services;
using QPS.Application.Features.Schedules.DTOs;
using QPS.Application.Features.Schedules.Queries;
using QPS.Domain.Entities;
using QPS.Domain.Enums;
using QPS.Domain.Scheduling;
using DayOfWeek = QPS.Domain.Enums.DayOfWeek;

namespace QPS.Application.Features.Schedules.Commands;

public record CreateScheduleSlotCommand(
    Guid? UserId,
    Guid? SemesterId,
    DayOfWeek Day,
    int SlotNumber,
    Guid? CourseId,
    string? CourseName,
    SlotType SlotType,
    string Room) : IRequest<Result<ScheduleChangeDto>>;

public record UpdateScheduleSlotCommand(
    Guid Id,
    DayOfWeek Day,
    int SlotNumber,
    Guid? CourseId,
    string? CourseName,
    SlotType SlotType,
    string Room) : IRequest<Result<ScheduleChangeDto>>;

public record DeleteScheduleSlotCommand(Guid Id) : IRequest<Result>;

public class CreateScheduleSlotValidator : AbstractValidator<CreateScheduleSlotCommand>
{
    public CreateScheduleSlotValidator()
    {
        RuleFor(x => x.Day).IsInEnum().NotEqual(DayOfWeek.Friday).WithMessage("Friday is a day off for everyone.");
        RuleFor(x => x.SlotNumber).InclusiveBetween(1, 5).WithMessage("Choose a slot from 1 to 5.");
        RuleFor(x => x.CourseName).NotEmpty().When(x => !x.CourseId.HasValue).WithMessage("Choose a course.");
        RuleFor(x => x.CourseName).MaximumLength(200);
        RuleFor(x => x.SlotType).IsInEnum();
        RuleFor(x => x.Room).NotEmpty().MaximumLength(50);
    }
}

public class UpdateScheduleSlotValidator : AbstractValidator<UpdateScheduleSlotCommand>
{
    public UpdateScheduleSlotValidator()
    {
        RuleFor(x => x.Day).IsInEnum().NotEqual(DayOfWeek.Friday).WithMessage("Friday is a day off for everyone.");
        RuleFor(x => x.SlotNumber).InclusiveBetween(1, 5).WithMessage("Choose a slot from 1 to 5.");
        RuleFor(x => x.CourseName).NotEmpty().When(x => !x.CourseId.HasValue).WithMessage("Choose a course.");
        RuleFor(x => x.CourseName).MaximumLength(200);
        RuleFor(x => x.SlotType).IsInEnum();
        RuleFor(x => x.Room).NotEmpty().MaximumLength(50);
    }
}

public class ScheduleCommandHandlers :
    IRequestHandler<CreateScheduleSlotCommand, Result<ScheduleChangeDto>>,
    IRequestHandler<UpdateScheduleSlotCommand, Result<ScheduleChangeDto>>,
    IRequestHandler<DeleteScheduleSlotCommand, Result>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly INotificationService _notifications;
    private readonly IClock _clock;

    public ScheduleCommandHandlers(IApplicationDbContext db, ICurrentUserService currentUser,
        INotificationService notifications, IClock clock)
    {
        _db = db;
        _currentUser = currentUser;
        _notifications = notifications;
        _clock = clock;
    }

    public async Task<Result<ScheduleChangeDto>> Handle(CreateScheduleSlotCommand request, CancellationToken ct)
    {
        var userId = request.UserId ?? _currentUser.RequireUserId();
        var ta = await LoadTaAsync(userId, ct);
        EnsureCanEdit(ta);
        var semester = await _db.ResolveSemesterAsync(request.SemesterId, ct);

        var problem = await ValidateSlotAsync(ta, semester.Id, request.Day, request.SlotNumber, null, ct);
        if (problem is not null) return Result<ScheduleChangeDto>.Fail(problem);

        var (courseId, courseName) = await ResolveCourseAsync(request.CourseId, request.CourseName, ct);
        var slotDef = SlotCatalog.Get(request.SlotNumber)!;
        var slot = new TAScheduleSlot
        {
            UserId = ta.Id,
            SemesterId = semester.Id,
            DayOfWeek = request.Day,
            SlotNumber = request.SlotNumber,
            CourseId = courseId,
            CourseName = courseName,
            SlotType = request.SlotType,
            Room = NameKey.Clean(request.Room),
            StartTime = slotDef.Start,
            EndTime = slotDef.End
        };
        _db.TAScheduleSlots.Add(slot);

        var warnings = await FindProctoringClashesAsync(ta, semester.Id, slot, ct);
        if (!_currentUser.IsAdmin)
            await _notifications.NotifyAdminsAsync($"TA {ta.FullName} added a slot on {request.Day} at Slot {request.SlotNumber}.",
                NotificationType.Schedule, $"/schedules?ta={ta.Id}", ct);
        await NotifyClashesAsync(ta, warnings, ct);

        await _db.SaveChangesAsync(ct);
        slot.User = ta;
        return Result<ScheduleChangeDto>.Ok(new ScheduleChangeDto(GetSchedulesHandler.ToDto(slot), warnings), "Slot added.");
    }

    public async Task<Result<ScheduleChangeDto>> Handle(UpdateScheduleSlotCommand request, CancellationToken ct)
    {
        var slot = await _db.TAScheduleSlots.FirstOrDefaultAsync(s => s.Id == request.Id, ct)
                   ?? throw new NotFoundException("Schedule slot", request.Id);
        var ta = await LoadTaAsync(slot.UserId, ct);
        EnsureCanEdit(ta);

        var problem = await ValidateSlotAsync(ta, slot.SemesterId, request.Day, request.SlotNumber, slot.Id, ct);
        if (problem is not null) return Result<ScheduleChangeDto>.Fail(problem);

        var (courseId, courseName) = await ResolveCourseAsync(request.CourseId, request.CourseName, ct);
        var slotDef = SlotCatalog.Get(request.SlotNumber)!;
        var before = $"{slot.DayOfWeek} at Slot {slot.SlotNumber}";
        slot.DayOfWeek = request.Day;
        slot.SlotNumber = request.SlotNumber;
        slot.CourseId = courseId;
        slot.CourseName = courseName;
        slot.SlotType = request.SlotType;
        slot.Room = NameKey.Clean(request.Room);
        slot.StartTime = slotDef.Start;
        slot.EndTime = slotDef.End;

        var warnings = await FindProctoringClashesAsync(ta, slot.SemesterId, slot, ct);
        if (!_currentUser.IsAdmin)
            await _notifications.NotifyAdminsAsync(
                $"TA {ta.FullName} changed a slot from {before} to {request.Day} at Slot {request.SlotNumber}.",
                NotificationType.Schedule, $"/schedules?ta={ta.Id}", ct);
        await NotifyClashesAsync(ta, warnings, ct);

        await _db.SaveChangesAsync(ct);
        slot.User = ta;
        return Result<ScheduleChangeDto>.Ok(new ScheduleChangeDto(GetSchedulesHandler.ToDto(slot), warnings), "Slot updated.");
    }

    public async Task<Result> Handle(DeleteScheduleSlotCommand request, CancellationToken ct)
    {
        var slot = await _db.TAScheduleSlots.FirstOrDefaultAsync(s => s.Id == request.Id, ct)
                   ?? throw new NotFoundException("Schedule slot", request.Id);
        var ta = await LoadTaAsync(slot.UserId, ct);
        EnsureCanEdit(ta);

        _db.TAScheduleSlots.Remove(slot);
        if (!_currentUser.IsAdmin)
            await _notifications.NotifyAdminsAsync($"TA {ta.FullName} removed a slot on {slot.DayOfWeek} at Slot {slot.SlotNumber}.",
                NotificationType.Schedule, $"/schedules?ta={ta.Id}", ct);
        await _db.SaveChangesAsync(ct);
        return Result.Ok("Slot removed.");
    }

    private async Task<User> LoadTaAsync(Guid userId, CancellationToken ct)
    {
        var ta = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId && !u.IsDeleted, ct)
                 ?? throw new NotFoundException("TA", userId);
        if (ta.Role != UserRole.TA) throw new ForbiddenAccessException("Only TAs have teaching schedules.");
        return ta;
    }

    private void EnsureCanEdit(User ta)
    {
        if (_currentUser.IsAdmin) return;
        if (_currentUser.UserId != ta.Id) throw new ForbiddenAccessException("You can only edit your own schedule.");
        if (!ta.CanEditSchedule) throw new ForbiddenAccessException("Schedule editing is turned off for your account. Ask an administrator.");
    }

    private async Task<string?> ValidateSlotAsync(User ta, Guid semesterId, DayOfWeek day, int slotNumber, Guid? ignoreId, CancellationToken ct)
    {
        if (ta.DaysOff.HasFlag(AcademicCalendar.ToFlag(day)))
            return $"{day} is {ta.FullName}'s day off. Change the days off first.";
        var taken = await _db.TAScheduleSlots.AnyAsync(s => s.UserId == ta.Id && s.SemesterId == semesterId
            && s.DayOfWeek == day && s.SlotNumber == slotNumber && s.Id != ignoreId, ct);
        return taken ? $"{ta.FullName} already has Slot {slotNumber} on {day}." : null;
    }

    private async Task<(Guid? Id, string Name)> ResolveCourseAsync(Guid? courseId, string? courseName, CancellationToken ct)
    {
        if (courseId.HasValue)
        {
            var course = await _db.Courses.FirstOrDefaultAsync(c => c.Id == courseId.Value, ct)
                         ?? throw new NotFoundException("Course", courseId.Value);
            return (course.Id, course.Name);
        }

        var name = NameKey.Clean(courseName);
        var key = NameKey.For(name);
        var match = await _db.Courses.FirstOrDefaultAsync(c => c.NormalizedName == key, ct);
        return match is null ? (null, name) : (match.Id, match.Name);
    }

    /// <summary>Upcoming proctoring sessions that now collide with this teaching slot.</summary>
    private async Task<List<string>> FindProctoringClashesAsync(User ta, Guid semesterId, TAScheduleSlot slot, CancellationToken ct)
    {
        var today = _clock.Today;
        var sessions = await _db.ProctorAssignments.AsNoTracking()
            .Where(a => a.UserId == ta.Id && a.QuizLocation.Quiz.SemesterId == semesterId && a.QuizLocation.Quiz.QuizDate >= today)
            .Select(a => new { a.QuizLocation.Quiz.QuizDate, a.QuizLocation.Quiz.StartTime, a.QuizLocation.Quiz.EndTime, Course = a.QuizLocation.Quiz.Course.Name })
            .ToListAsync(ct);

        return sessions
            .Where(s => AcademicCalendar.ToDomainDay(s.QuizDate) == slot.DayOfWeek
                        && QuizTiming.Overlaps(s.StartTime, s.EndTime, slot.StartTime, slot.EndTime))
            .OrderBy(s => s.QuizDate)
            .Select(s => $"Clashes with proctoring {s.Course} on {Format.Date(s.QuizDate)} ({Format.TimeRange(s.StartTime, s.EndTime)}).")
            .ToList();
    }

    private async Task NotifyClashesAsync(User ta, List<string> warnings, CancellationToken ct)
    {
        if (warnings.Count == 0) return;
        await _notifications.NotifyAdminsAsync(
            $"{ta.FullName}'s new teaching slot clashes with {warnings.Count} upcoming proctoring session{(warnings.Count == 1 ? "" : "s")}.",
            NotificationType.Schedule, "/quizzes?status=Upcoming", ct);
    }
}
