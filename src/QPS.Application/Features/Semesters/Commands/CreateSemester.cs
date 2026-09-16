using FluentValidation;
using MediatR;
using QPS.Application.Common.Interfaces;
using QPS.Application.Common.Models;
using QPS.Application.Features.Semesters.DTOs;
using QPS.Domain.Entities;

namespace QPS.Application.Features.Semesters.Commands;

public class CreateSemesterValidator : AbstractValidator<CreateSemesterCommand>
{
    public CreateSemesterValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        RuleFor(x => x.EndDate).GreaterThan(x => x.StartDate);
    }
}

public class CreateSemesterHandler : IRequestHandler<CreateSemesterCommand, Result<SemesterDto>>
{
    private readonly IApplicationDbContext _context;

    public CreateSemesterHandler(IApplicationDbContext context) => _context = context;

    public async Task<Result<SemesterDto>> Handle(
        CreateSemesterCommand request, CancellationToken cancellationToken)
    {
        var semester = new Semester
        {
            Name = request.Name.Trim(),
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            IsActive = true
        };

        _context.Semesters.Add(semester);
        await _context.SaveChangesAsync(cancellationToken);

        var dto = new SemesterDto(semester.Id, semester.Name, semester.StartDate, semester.EndDate, semester.IsActive);
        return Result<SemesterDto>.Ok(dto, "Semester created successfully");
    }
}
