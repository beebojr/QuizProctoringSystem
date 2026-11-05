using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using QPS.Application.Common.Interfaces;
using QPS.Application.Common.Models;
using QPS.Application.Common.Services;
using QPS.Application.Features.Quizzes.Commands;
using QPS.Application.Features.Quizzes.DTOs;
using QPS.Domain.Entities;
using QPS.Domain.Enums;
using QPS.Domain.Scheduling;

namespace QPS.Application.Features.Quizzes.Import;

public record ImportCommitCommand(
    Guid? SemesterId,
    List<ImportQuizInput> Quizzes,
    bool CreateMissingCourses = true,
    bool AddReserve = false,
    bool AutoAssign = false) : IRequest<Result<ImportCommitResultDto>>;

public class ImportCommitValidator : AbstractValidator<ImportCommitCommand>
{
    public ImportCommitValidator()
    {
        RuleFor(x => x.Quizzes).NotEmpty().WithMessage("There is nothing to import.");
        RuleForEach(x => x.Quizzes).ChildRules(q =>
        {
            q.RuleFor(x => x.CourseName).NotEmpty().MaximumLength(200);
            q.RuleFor(x => x).Must(x => x.EndTime > x.StartTime).WithMessage("A quiz ends before it starts.");
            q.RuleFor(x => x.Locations).NotEmpty();
            q.RuleForEach(x => x.Locations).ChildRules(l =>
            {
                l.RuleFor(x => x.RoomName).NotEmpty().MaximumLength(50);
                l.RuleFor(x => x.ProctorsNeeded).InclusiveBetween(1, 10);
                l.RuleFor(x => x.Group).MaximumLength(40);
            });
        });
    }
}

public class ImportCommitHandler : IRequestHandler<ImportCommitCommand, Result<ImportCommitResultDto>>
{
    private readonly IApplicationDbContext _db;
    private readonly AutoAssigner _autoAssigner;
    private readonly INotificationService _notifications;

    public ImportCommitHandler(IApplicationDbContext db, AutoAssigner autoAssigner, INotificationService notifications)
    {
        _db = db;
        _autoAssigner = autoAssigner;
        _notifications = notifications;
    }

    public async Task<Result<ImportCommitResultDto>> Handle(ImportCommitCommand request, CancellationToken ct)
    {
        var semester = await _db.ResolveSemesterAsync(request.SemesterId, ct);
        var settings = await _db.GetSettingsAsync(ct);
        var courses = (await _db.Courses.ToListAsync(ct)).GroupBy(c => c.NormalizedName).ToDictionary(g => g.Key, g => g.First());
        var roomKeys = (await _db.Rooms.Select(r => r.NormalizedName).ToListAsync(ct)).ToHashSet();
        var existingQuizzes = await _db.Quizzes.Include(q => q.Locations).Where(q => q.SemesterId == semester.Id).ToListAsync(ct);

        var errors = new List<string>();
        var coursesCreated = new List<string>();
        var touched = new HashSet<Guid>();
        int created = 0, updated = 0, roomsAdded = 0;

        foreach (var input in request.Quizzes)
        {
            var courseName = NameKey.Clean(input.CourseName);
            var courseKey = NameKey.For(courseName);
            if (input.Date.DayOfWeek == System.DayOfWeek.Friday)
            {
                errors.Add($"{courseName} on {Format.Date(input.Date)} skipped: Friday is a day off for all TAs.");
                continue;
            }
            if (!courses.TryGetValue(courseKey, out var course))
            {
                if (!request.CreateMissingCourses)
                {
                    errors.Add($"{courseName} on {Format.Date(input.Date)} skipped: the course doesn't exist.");
                    continue;
                }
                course = new Course { Name = courseName, NormalizedName = courseKey, Department = settings.DepartmentName };
                _db.Courses.Add(course);
                courses[courseKey] = course;
                coursesCreated.Add(courseName);
            }

            var quiz = existingQuizzes.FirstOrDefault(q => q.CourseId == course.Id && q.QuizDate == input.Date && q.StartTime == input.StartTime);
            var isNew = quiz is null;
            if (quiz is null)
            {
                quiz = new Quiz
                {
                    Course = course,
                    CourseId = course.Id,
                    SemesterId = semester.Id,
                    ExamType = input.ExamType,
                    QuizDate = input.Date,
                    StartTime = input.StartTime,
                    EndTime = input.EndTime,
                    SlotNumber = SlotCatalog.FromTimes(input.StartTime, input.EndTime)?.Number,
                    WeekNumber = input.WeekNumber is > 0 and < 60 ? input.WeekNumber.Value : AcademicCalendar.WeekNumber(semester.StartDate, input.Date),
                    Source = QuizSource.Import,
                    AutoAssign = request.AutoAssign
                };
                _db.Quizzes.Add(quiz);
                existingQuizzes.Add(quiz);
            }

            var order = quiz.Locations.Where(l => !l.IsReserve).Select(l => l.SortOrder).DefaultIfEmpty(-1).Max() + 1;
            var added = 0;
            foreach (var loc in input.Locations)
            {
                var roomName = NameKey.Clean(loc.RoomName);
                var key = NameKey.For(roomName);
                if (quiz.Locations.Any(l => NameKey.For(l.RoomName) == key)) continue;
                quiz.Locations.Add(new QuizLocation
                {
                    RoomName = roomName,
                    Group = string.IsNullOrWhiteSpace(loc.Group) ? null : NameKey.Clean(loc.Group),
                    ProctorsNeeded = loc.ProctorsNeeded,
                    SortOrder = order++
                });
                added++;
                if (roomKeys.Add(key))
                {
                    _db.Rooms.Add(new Room { Name = roomName, NormalizedName = key, DefaultProctors = loc.ProctorsNeeded });
                    roomsAdded++;
                }
            }

            if (request.AddReserve && !quiz.Locations.Any(l => l.IsReserve))
            {
                quiz.Locations.Add(QuizCommandHandlers.ReserveLocation(settings));
                added++;
            }
            quiz.HasReserve = quiz.Locations.Any(l => l.IsReserve);

            if (isNew) created++;
            else if (added > 0) updated++;
            if (isNew || added > 0) touched.Add(quiz.Id);
        }

        await _db.SaveChangesAsync(ct);

        AutoAssignSummaryDto? auto = null;
        if (request.AutoAssign && touched.Count > 0)
        {
            var run = await _autoAssigner.PlanAsync(semester.Id, q => q.Where(x => touched.Contains(x.Id)), ct);
            auto = await _autoAssigner.ApplyAsync(run, ct);
        }

        if (created + updated > 0)
        {
            await _notifications.NotifyAdminsAsync(
                $"Quiz calendar imported: {created} new quiz{(created == 1 ? "" : "zes")}, {updated} updated.",
                NotificationType.Quiz, "/quizzes", ct);
            await _db.SaveChangesAsync(ct);
        }

        return Result<ImportCommitResultDto>.Ok(
            new ImportCommitResultDto(created, updated, roomsAdded, coursesCreated, errors, auto),
            $"Imported {created} new quiz{(created == 1 ? "" : "zes")}" + (updated > 0 ? $" and updated {updated}." : "."));
    }
}
