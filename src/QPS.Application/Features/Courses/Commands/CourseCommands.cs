using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using QPS.Application.Common.Exceptions;
using QPS.Application.Common.Interfaces;
using QPS.Application.Common.Models;
using QPS.Application.Common.Services;
using QPS.Application.Features.Courses.DTOs;
using QPS.Domain.Entities;
using QPS.Domain.Scheduling;

namespace QPS.Application.Features.Courses.Commands;

public record CreateCourseCommand(string Name, string? Code = null, string? Department = null) : IRequest<Result<CourseDto>>;
public record UpdateCourseCommand(Guid Id, string Name, string? Code = null) : IRequest<Result<CourseDto>>;
public record DeleteCourseCommand(Guid Id) : IRequest<Result>;

public class CreateCourseValidator : AbstractValidator<CreateCourseCommand>
{
    public CreateCourseValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Code).MaximumLength(30);
        RuleFor(x => x.Department).MaximumLength(50);
    }
}

public class UpdateCourseValidator : AbstractValidator<UpdateCourseCommand>
{
    public UpdateCourseValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Code).MaximumLength(30);
    }
}

public class CourseCommandHandlers :
    IRequestHandler<CreateCourseCommand, Result<CourseDto>>,
    IRequestHandler<UpdateCourseCommand, Result<CourseDto>>,
    IRequestHandler<DeleteCourseCommand, Result>
{
    private readonly IApplicationDbContext _db;

    public CourseCommandHandlers(IApplicationDbContext db) => _db = db;

    public async Task<Result<CourseDto>> Handle(CreateCourseCommand request, CancellationToken ct)
    {
        var department = string.IsNullOrWhiteSpace(request.Department)
            ? (await _db.GetSettingsAsync(ct)).DepartmentName
            : request.Department.Trim();
        var name = NameKey.Clean(request.Name);
        var key = NameKey.For(name);

        if (await _db.Courses.AnyAsync(c => c.Department == department && c.NormalizedName == key, ct))
            return Result<CourseDto>.Fail($"\"{name}\" already exists.");

        var course = new Course { Name = name, NormalizedName = key, Code = Blank(request.Code), Department = department };
        _db.Courses.Add(course);
        await _db.SaveChangesAsync(ct);
        return Result<CourseDto>.Ok(new CourseDto(course.Id, course.Name, course.Code, course.Department, 0, 0), "Course added.");
    }

    public async Task<Result<CourseDto>> Handle(UpdateCourseCommand request, CancellationToken ct)
    {
        var course = await _db.Courses.FirstOrDefaultAsync(c => c.Id == request.Id, ct)
                     ?? throw new NotFoundException("Course", request.Id);
        var name = NameKey.Clean(request.Name);
        var key = NameKey.For(name);
        if (await _db.Courses.AnyAsync(c => c.Id != course.Id && c.Department == course.Department && c.NormalizedName == key, ct))
            return Result<CourseDto>.Fail($"\"{name}\" already exists.");

        course.Name = name;
        course.NormalizedName = key;
        course.Code = Blank(request.Code);

        // Keep the course name shown in TA schedules in sync.
        foreach (var slot in await _db.TAScheduleSlots.Where(s => s.CourseId == course.Id).ToListAsync(ct))
            slot.CourseName = name;

        await _db.SaveChangesAsync(ct);
        return Result<CourseDto>.Ok(new CourseDto(course.Id, course.Name, course.Code, course.Department, 0, 0), "Course updated.");
    }

    public async Task<Result> Handle(DeleteCourseCommand request, CancellationToken ct)
    {
        var course = await _db.Courses.FirstOrDefaultAsync(c => c.Id == request.Id, ct)
                     ?? throw new NotFoundException("Course", request.Id);
        var quizCount = await _db.Quizzes.CountAsync(q => q.CourseId == course.Id, ct);
        if (quizCount > 0)
            return Result.Fail($"\"{course.Name}\" has {quizCount} quiz{(quizCount == 1 ? "" : "zes")}. Delete them first.");

        foreach (var slot in await _db.TAScheduleSlots.Where(s => s.CourseId == course.Id).ToListAsync(ct))
            slot.CourseId = null;

        _db.Courses.Remove(course);
        await _db.SaveChangesAsync(ct);
        return Result.Ok("Course deleted.");
    }

    private static string? Blank(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
