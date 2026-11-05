using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using QPS.Application.Common.Exceptions;
using QPS.Application.Common.Interfaces;
using QPS.Application.Common.Models;
using QPS.Application.Common.Services;
using QPS.Application.Features.Quizzes.DTOs;
using QPS.Domain.Entities;
using QPS.Domain.Enums;
using QPS.Domain.Scheduling;

namespace QPS.Application.Features.Quizzes.Commands;

public record CreateQuizCommand(
    Guid CourseId,
    Guid? SemesterId,
    ExamType ExamType,
    DateOnly QuizDate,
    TimeOnly StartTime,
    TimeOnly EndTime,
    string? Notes,
    bool AutoAssign,
    bool HasReserve,
    List<LocationInput> Locations,
    int? WeekNumber = null) : IRequest<Result<QuizSaveResultDto>>;

public record UpdateQuizCommand(
    Guid Id,
    Guid CourseId,
    ExamType ExamType,
    DateOnly QuizDate,
    TimeOnly StartTime,
    TimeOnly EndTime,
    string? Notes,
    bool AutoAssign,
    List<LocationInput> Locations,
    int? WeekNumber = null) : IRequest<Result<QuizSaveResultDto>>;

public record DeleteQuizCommand(Guid Id) : IRequest<Result>;

public class LocationInputValidator : AbstractValidator<LocationInput>
{
    public LocationInputValidator()
    {
        RuleFor(x => x.RoomName).NotEmpty().When(x => !x.IsReserve).WithMessage("Every location needs a room name.").MaximumLength(50);
        RuleFor(x => x.Group).MaximumLength(40);
        RuleFor(x => x.ProctorsNeeded).InclusiveBetween(1, 10).WithMessage("Proctors per room must be between 1 and 10.");
    }
}

internal static class QuizRules
{
    public static bool NotFriday(DateOnly d) => d.DayOfWeek != System.DayOfWeek.Friday;

    public static bool HasRoom(List<LocationInput>? l) => l is not null && l.Any(x => !x.IsReserve);

    public static bool UniqueRooms(List<LocationInput>? l) =>
        l is null || l.Select(x => x.IsReserve ? "#reserve" : NameKey.For(x.RoomName)).Distinct().Count() == l.Count;

    public const string FridayMessage = "Quizzes can't be on a Friday (day off for all TAs).";
    public const string EndMessage = "The end time must be after the start time.";
}

public class CreateQuizValidator : AbstractValidator<CreateQuizCommand>
{
    public CreateQuizValidator()
    {
        RuleFor(x => x.CourseId).NotEmpty().WithMessage("Choose a course.");
        RuleFor(x => x.QuizDate).Must(QuizRules.NotFriday).WithMessage(QuizRules.FridayMessage);
        RuleFor(x => x.EndTime).GreaterThan(x => x.StartTime).WithMessage(QuizRules.EndMessage);
        RuleFor(x => x.Notes).MaximumLength(500);
        RuleFor(x => x.Locations).Must(QuizRules.HasRoom).WithMessage("Add at least one room.")
            .Must(QuizRules.UniqueRooms).WithMessage("Each room can only be listed once.");
        RuleForEach(x => x.Locations).SetValidator(new LocationInputValidator());
    }
}

public class UpdateQuizValidator : AbstractValidator<UpdateQuizCommand>
{
    public UpdateQuizValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.CourseId).NotEmpty().WithMessage("Choose a course.");
        RuleFor(x => x.QuizDate).Must(QuizRules.NotFriday).WithMessage(QuizRules.FridayMessage);
        RuleFor(x => x.EndTime).GreaterThan(x => x.StartTime).WithMessage(QuizRules.EndMessage);
        RuleFor(x => x.Notes).MaximumLength(500);
        RuleFor(x => x.Locations).Must(QuizRules.HasRoom).WithMessage("Add at least one room.")
            .Must(QuizRules.UniqueRooms).WithMessage("Each room can only be listed once.");
        RuleForEach(x => x.Locations).SetValidator(new LocationInputValidator());
    }
}

public class QuizCommandHandlers :
    IRequestHandler<CreateQuizCommand, Result<QuizSaveResultDto>>,
    IRequestHandler<UpdateQuizCommand, Result<QuizSaveResultDto>>,
    IRequestHandler<DeleteQuizCommand, Result>
{
    private readonly IApplicationDbContext _db;
    private readonly IClock _clock;
    private readonly AssignmentWorkspace _workspace;
    private readonly AutoAssigner _autoAssigner;
    private readonly INotificationService _notifications;

    public QuizCommandHandlers(IApplicationDbContext db, IClock clock, AssignmentWorkspace workspace,
        AutoAssigner autoAssigner, INotificationService notifications)
    {
        _db = db;
        _clock = clock;
        _workspace = workspace;
        _autoAssigner = autoAssigner;
        _notifications = notifications;
    }

    public async Task<Result<QuizSaveResultDto>> Handle(CreateQuizCommand request, CancellationToken ct)
    {
        var semester = await _db.ResolveSemesterAsync(request.SemesterId, ct);
        var settings = await _db.GetSettingsAsync(ct);
        var course = await _db.Courses.FirstOrDefaultAsync(c => c.Id == request.CourseId, ct)
                     ?? throw new NotFoundException("Course", request.CourseId);

        var warnings = await CommonWarningsAsync(semester, course, request.QuizDate, request.StartTime, request.EndTime, null, ct);

        var quiz = new Quiz
        {
            CourseId = course.Id,
            SemesterId = semester.Id,
            ExamType = request.ExamType,
            QuizDate = request.QuizDate,
            StartTime = request.StartTime,
            EndTime = request.EndTime,
            SlotNumber = SlotCatalog.FromTimes(request.StartTime, request.EndTime)?.Number,
            WeekNumber = request.WeekNumber is > 0 and < 60 ? request.WeekNumber.Value : AcademicCalendar.WeekNumber(semester.StartDate, request.QuizDate),
            Notes = string.IsNullOrWhiteSpace(request.Notes) ? null : request.Notes.Trim(),
            AutoAssign = request.AutoAssign,
            Source = QuizSource.Manual
        };

        var order = 0;
        foreach (var loc in request.Locations)
            quiz.Locations.Add(NewLocation(loc, order++));

        if (request.HasReserve && !quiz.Locations.Any(l => l.IsReserve))
            quiz.Locations.Add(ReserveLocation(settings));
        quiz.HasReserve = quiz.Locations.Any(l => l.IsReserve);

        _db.Quizzes.Add(quiz);
        await RegisterRoomsAsync(quiz.Locations.Where(l => !l.IsReserve).Select(l => l.RoomName), settings, ct);
        await _db.SaveChangesAsync(ct);

        AutoAssignSummaryDto? auto = null;
        if (request.AutoAssign)
        {
            if (quiz.QuizDate.ToDateTime(quiz.StartTime) <= _clock.Now)
                warnings.Add("Auto-assign skipped: this quiz has already started.");
            else
            {
                var run = await _autoAssigner.PlanAsync(semester.Id, q => q.Where(x => x.Id == quiz.Id), ct);
                auto = await _autoAssigner.ApplyAsync(run, ct);
            }
        }

        var dto = await LoadDtoAsync(quiz.Id, ct);
        var message = auto is null ? "Quiz created." :
            auto.Missing == 0 ? $"Quiz created and fully staffed ({auto.Assigned} proctors)."
                              : $"Quiz created. {auto.Assigned} proctors assigned, {auto.Missing} seat(s) still empty.";
        return Result<QuizSaveResultDto>.Ok(new QuizSaveResultDto(dto, warnings, auto), message);
    }

    public async Task<Result<QuizSaveResultDto>> Handle(UpdateQuizCommand request, CancellationToken ct)
    {
        var quiz = await _db.Quizzes
            .Include(q => q.Course)
            .Include(q => q.Semester)
            .Include(q => q.Locations).ThenInclude(l => l.Assignments).ThenInclude(a => a.User)
            .FirstOrDefaultAsync(q => q.Id == request.Id, ct)
            ?? throw new NotFoundException("Quiz", request.Id);
        var settings = await _db.GetSettingsAsync(ct);
        var course = quiz.CourseId == request.CourseId
            ? quiz.Course
            : await _db.Courses.FirstOrDefaultAsync(c => c.Id == request.CourseId, ct) ?? throw new NotFoundException("Course", request.CourseId);

        var errors = new List<string>();
        var incoming = request.Locations.ToList();
        foreach (var input in incoming.Where(i => i.Id.HasValue))
        {
            var existing = quiz.Locations.FirstOrDefault(l => l.Id == input.Id!.Value);
            if (existing is null)
                errors.Add($"Location {input.RoomName} no longer exists. Reload the quiz.");
            else if (input.ProctorsNeeded < existing.Assignments.Count)
                errors.Add($"{existing.RoomName} has {existing.Assignments.Count} proctors assigned. Remove some before lowering it to {input.ProctorsNeeded}.");
        }
        if (errors.Count > 0) return Result<QuizSaveResultDto>.Fail(errors);

        var timeChanged = quiz.QuizDate != request.QuizDate || quiz.StartTime != request.StartTime || quiz.EndTime != request.EndTime;
        var warnings = await CommonWarningsAsync(quiz.Semester, course, request.QuizDate, request.StartTime, request.EndTime, quiz.Id, ct);
        var before = $"{Format.Date(quiz.QuizDate)}, {Format.TimeRange(quiz.StartTime, quiz.EndTime)}";

        quiz.CourseId = course.Id;
        quiz.Course = course;
        quiz.ExamType = request.ExamType;
        quiz.QuizDate = request.QuizDate;
        quiz.StartTime = request.StartTime;
        quiz.EndTime = request.EndTime;
        quiz.SlotNumber = SlotCatalog.FromTimes(request.StartTime, request.EndTime)?.Number;
        quiz.WeekNumber = request.WeekNumber is > 0 and < 60 ? request.WeekNumber.Value : AcademicCalendar.WeekNumber(quiz.Semester.StartDate, request.QuizDate);
        quiz.Notes = string.IsNullOrWhiteSpace(request.Notes) ? null : request.Notes.Trim();
        quiz.AutoAssign = request.AutoAssign;

        // Removed locations take their assignments with them.
        var keepIds = incoming.Where(i => i.Id.HasValue).Select(i => i.Id!.Value).ToHashSet();
        var upcoming = quiz.QuizDate.ToDateTime(quiz.StartTime) > _clock.Now;
        foreach (var removed in quiz.Locations.Where(l => !keepIds.Contains(l.Id)).ToList())
        {
            if (upcoming)
                foreach (var a in removed.Assignments)
                    _notifications.Notify(a.UserId,
                        $"You are no longer proctoring {course.Name} on {Format.Date(quiz.QuizDate)} ({removed.RoomName} was removed from the quiz).",
                        NotificationType.Assignment, "/my-assignments");
            _db.QuizLocations.Remove(removed);
            quiz.Locations.Remove(removed);
        }

        var order = 0;
        foreach (var input in incoming)
        {
            if (input.Id.HasValue)
            {
                var loc = quiz.Locations.First(l => l.Id == input.Id.Value);
                loc.RoomName = input.IsReserve ? "Reserved" : NameKey.Clean(input.RoomName);
                loc.Group = string.IsNullOrWhiteSpace(input.Group) ? null : NameKey.Clean(input.Group);
                loc.ProctorsNeeded = input.ProctorsNeeded;
                loc.IsReserve = input.IsReserve;
                loc.SortOrder = input.IsReserve ? 999 : order++;
            }
            else
            {
                var loc = NewLocation(input, input.IsReserve ? 999 : order++);
                loc.QuizId = quiz.Id;
                _db.QuizLocations.Add(loc);
                if (!quiz.Locations.Contains(loc)) quiz.Locations.Add(loc);
            }
        }
        quiz.HasReserve = quiz.Locations.Any(l => l.IsReserve);

        if (timeChanged && upcoming)
        {
            foreach (var a in quiz.Locations.SelectMany(l => l.Assignments))
                _notifications.Notify(a.UserId,
                    $"{course.Name} moved from {before} to {Format.Date(quiz.QuizDate)}, {Format.TimeRange(quiz.StartTime, quiz.EndTime)}. You are still proctoring it.",
                    NotificationType.Quiz, "/my-assignments");
        }

        await RegisterRoomsAsync(quiz.Locations.Where(l => !l.IsReserve).Select(l => l.RoomName), settings, ct);
        await _db.SaveChangesAsync(ct);

        var dto = await LoadDtoAsync(quiz.Id, ct);
        var flagged = dto.Locations.SelectMany(l => l.Proctors).Where(p => p.Warnings.Count > 0).ToList();
        warnings.AddRange(flagged.Select(p => $"{p.Name}: {p.Warnings[0]}"));
        return Result<QuizSaveResultDto>.Ok(new QuizSaveResultDto(dto, warnings, null),
            flagged.Count == 0 ? "Quiz updated." : $"Quiz updated. {flagged.Count} proctor(s) now break a rule — review them.");
    }

    public async Task<Result> Handle(DeleteQuizCommand request, CancellationToken ct)
    {
        var quiz = await _db.Quizzes
            .Include(q => q.Course)
            .Include(q => q.Locations).ThenInclude(l => l.Assignments)
            .FirstOrDefaultAsync(q => q.Id == request.Id, ct)
            ?? throw new NotFoundException("Quiz", request.Id);

        if (quiz.QuizDate.ToDateTime(quiz.StartTime) > _clock.Now)
        {
            foreach (var userId in quiz.Locations.SelectMany(l => l.Assignments).Select(a => a.UserId).Distinct())
                _notifications.Notify(userId,
                    $"{quiz.Course.Name} on {Format.Date(quiz.QuizDate)} was cancelled. You no longer need to proctor it.",
                    NotificationType.Quiz, "/my-assignments");
        }

        _db.Quizzes.Remove(quiz);
        await _db.SaveChangesAsync(ct);
        return Result.Ok("Quiz deleted.");
    }

    private static QuizLocation NewLocation(LocationInput input, int order) => new()
    {
        RoomName = input.IsReserve ? "Reserved" : NameKey.Clean(input.RoomName),
        Group = string.IsNullOrWhiteSpace(input.Group) ? null : NameKey.Clean(input.Group),
        ProctorsNeeded = input.ProctorsNeeded,
        IsReserve = input.IsReserve,
        SortOrder = input.IsReserve ? 999 : order
    };

    public static QuizLocation ReserveLocation(SystemSettings settings) => new()
    {
        RoomName = "Reserved",
        ProctorsNeeded = settings.ReserveProctors,
        IsReserve = true,
        SortOrder = 999
    };

    private async Task<List<string>> CommonWarningsAsync(Semester semester, Course course, DateOnly date,
        TimeOnly start, TimeOnly end, Guid? ignoreQuizId, CancellationToken ct)
    {
        var warnings = new List<string>();
        if (date < semester.StartDate || date > semester.EndDate)
            warnings.Add($"{Format.Date(date)} is outside {semester.Name} ({Format.Date(semester.StartDate)} – {Format.Date(semester.EndDate)}).");

        var sameDay = await _db.Quizzes.AsNoTracking()
            .Where(q => q.CourseId == course.Id && q.QuizDate == date && q.Id != ignoreQuizId)
            .Select(q => new { q.StartTime, q.EndTime })
            .ToListAsync(ct);
        if (sameDay.Any(q => QuizTiming.Overlaps(q.StartTime, q.EndTime, start, end)))
            warnings.Add($"{course.Name} already has a quiz at an overlapping time that day.");
        return warnings;
    }

    private async Task RegisterRoomsAsync(IEnumerable<string> rooms, SystemSettings settings, CancellationToken ct)
    {
        var keys = rooms.Select(NameKey.For).Where(k => k.Length > 0).ToHashSet();
        if (keys.Count == 0) return;
        var known = await _db.Rooms.Where(r => keys.Contains(r.NormalizedName)).Select(r => r.NormalizedName).ToListAsync(ct);
        foreach (var name in rooms.Where(r => !known.Contains(NameKey.For(r))).DistinctBy(NameKey.For))
            _db.Rooms.Add(new Room { Name = NameKey.Clean(name), NormalizedName = NameKey.For(name), DefaultProctors = settings.DefaultProctorsPerRoom });
    }

    private async Task<QuizDto> LoadDtoAsync(Guid quizId, CancellationToken ct)
    {
        var quiz = await _db.Quizzes.AsNoTracking()
            .Include(q => q.Course)
            .Include(q => q.Locations).ThenInclude(l => l.Assignments).ThenInclude(a => a.User)
            .FirstAsync(q => q.Id == quizId, ct);
        var context = await _workspace.LoadAsync(quiz.SemesterId, ct);
        return QuizMapper.ToDto(quiz, _clock.Now, context);
    }
}
