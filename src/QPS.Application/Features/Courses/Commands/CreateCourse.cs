using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using QPS.Application.Common.Interfaces;
using QPS.Application.Common.Models;
using QPS.Application.Features.Courses.DTOs;
using QPS.Domain.Entities;

namespace QPS.Application.Features.Courses.Commands;

public class CreateCourseValidator : AbstractValidator<CreateCourseCommand>
{
    private readonly IApplicationDbContext _context;

    public CreateCourseValidator(IApplicationDbContext context)
    {
        _context = context;
        RuleFor(x => x.Name)
            .NotEmpty().MaximumLength(200)
            .MustAsync(BeUniqueName).WithMessage("Course name already exists.");
        RuleFor(x => x.Department)
            .NotEmpty().MaximumLength(50);
    }

    private async Task<bool> BeUniqueName(string name, CancellationToken ct)
        => !await _context.Courses.AnyAsync(c => c.Name == name, ct);
}

public class CreateCourseHandler : IRequestHandler<CreateCourseCommand, Result<CourseDto>>
{
    private readonly IApplicationDbContext _context;

    public CreateCourseHandler(IApplicationDbContext context) => _context = context;

    public async Task<Result<CourseDto>> Handle(
        CreateCourseCommand request, CancellationToken cancellationToken)
    {
        var course = new Course
        {
            Name = request.Name.Trim(),
            Department = request.Department.Trim()
        };

        _context.Courses.Add(course);
        await _context.SaveChangesAsync(cancellationToken);

        var dto = new CourseDto(course.Id, course.Name, course.Department, course.CreatedAt);
        return Result<CourseDto>.Ok(dto, "Course created successfully");
    }
}