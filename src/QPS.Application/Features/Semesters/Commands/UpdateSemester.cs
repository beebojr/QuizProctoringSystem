using FluentValidation;
using MediatR;
using QPS.Application.Common.Interfaces;
using QPS.Application.Common.Models;
using QPS.Application.Features.Semesters.DTOs;

namespace QPS.Application.Features.Semesters.Commands;

public class UpdateSemesterValidator : AbstractValidator<UpdateSemesterCommand>
{
    public UpdateSemesterValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        RuleFor(x => x.EndDate).GreaterThan(x => x.StartDate);
    }
}

public class UpdateSemesterHandler : IRequestHandler<UpdateSemesterCommand, Result<SemesterDto>>
{
    private readonly IApplicationDbContext _context;

    public UpdateSemesterHandler(IApplicationDbContext context) => _context = context;

    public async Task<Result<SemesterDto>> Handle(
        UpdateSemesterCommand request, CancellationToken cancellationToken)
    {
        var semester = _context.Semesters.FirstOrDefault(s => s.Id == request.Id);
        if (semester == null)
            return Result<SemesterDto>.Fail("Semester not found");

        semester.Name = request.Name.Trim();
        semester.StartDate = request.StartDate;
        semester.EndDate = request.EndDate;
        semester.IsActive = request.IsActive;

        await _context.SaveChangesAsync(cancellationToken);

        var dto = new SemesterDto(semester.Id, semester.Name, semester.StartDate, semester.EndDate, semester.IsActive);
        return Result<SemesterDto>.Ok(dto, "Semester updated successfully");
    }
}
