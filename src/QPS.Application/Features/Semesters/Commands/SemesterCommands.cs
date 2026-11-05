using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using QPS.Application.Common.Exceptions;
using QPS.Application.Common.Interfaces;
using QPS.Application.Common.Models;
using QPS.Application.Features.Semesters.DTOs;
using QPS.Application.Features.Semesters.Queries;
using QPS.Domain.Entities;
using QPS.Domain.Scheduling;

namespace QPS.Application.Features.Semesters.Commands;

public record CreateSemesterCommand(string Name, DateOnly StartDate, DateOnly EndDate, bool MakeActive = true)
    : IRequest<Result<SemesterDto>>;

public record UpdateSemesterCommand(Guid Id, string Name, DateOnly StartDate, DateOnly EndDate)
    : IRequest<Result<SemesterDto>>;

public record ActivateSemesterCommand(Guid Id) : IRequest<Result<SemesterDto>>;

public class CreateSemesterValidator : AbstractValidator<CreateSemesterCommand>
{
    public CreateSemesterValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        RuleFor(x => x.EndDate).GreaterThan(x => x.StartDate).WithMessage("The semester must end after it starts.");
    }
}

public class UpdateSemesterValidator : AbstractValidator<UpdateSemesterCommand>
{
    public UpdateSemesterValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        RuleFor(x => x.EndDate).GreaterThan(x => x.StartDate).WithMessage("The semester must end after it starts.");
    }
}

public class SemesterCommandHandlers :
    IRequestHandler<CreateSemesterCommand, Result<SemesterDto>>,
    IRequestHandler<UpdateSemesterCommand, Result<SemesterDto>>,
    IRequestHandler<ActivateSemesterCommand, Result<SemesterDto>>
{
    private readonly IApplicationDbContext _db;
    private readonly IClock _clock;

    public SemesterCommandHandlers(IApplicationDbContext db, IClock clock)
    {
        _db = db;
        _clock = clock;
    }

    public async Task<Result<SemesterDto>> Handle(CreateSemesterCommand request, CancellationToken ct)
    {
        var semester = new Semester
        {
            Name = request.Name.Trim(),
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            IsActive = request.MakeActive || !await _db.Semesters.AnyAsync(ct)
        };
        if (semester.IsActive) await DeactivateAllAsync(ct);
        _db.Semesters.Add(semester);
        await _db.SaveChangesAsync(ct);
        return Result<SemesterDto>.Ok(GetSemestersHandler.ToDto(semester, _clock.Today), "Semester created.");
    }

    public async Task<Result<SemesterDto>> Handle(UpdateSemesterCommand request, CancellationToken ct)
    {
        var semester = await _db.Semesters.FirstOrDefaultAsync(s => s.Id == request.Id, ct)
                       ?? throw new NotFoundException("Semester", request.Id);
        var startChanged = semester.StartDate != request.StartDate;
        semester.Name = request.Name.Trim();
        semester.StartDate = request.StartDate;
        semester.EndDate = request.EndDate;

        if (startChanged)
        {
            // Week numbers follow the semester start date, so recompute them.
            // Imported quizzes keep the official week label from the university calendar.
            var quizzes = await _db.Quizzes.Where(q => q.SemesterId == semester.Id && q.Source == QPS.Domain.Enums.QuizSource.Manual).ToListAsync(ct);
            foreach (var q in quizzes)
                q.WeekNumber = AcademicCalendar.WeekNumber(semester.StartDate, q.QuizDate);
        }

        await _db.SaveChangesAsync(ct);
        return Result<SemesterDto>.Ok(GetSemestersHandler.ToDto(semester, _clock.Today), "Semester updated.");
    }

    public async Task<Result<SemesterDto>> Handle(ActivateSemesterCommand request, CancellationToken ct)
    {
        var semester = await _db.Semesters.FirstOrDefaultAsync(s => s.Id == request.Id, ct)
                       ?? throw new NotFoundException("Semester", request.Id);
        await DeactivateAllAsync(ct);
        semester.IsActive = true;
        await _db.SaveChangesAsync(ct);
        return Result<SemesterDto>.Ok(GetSemestersHandler.ToDto(semester, _clock.Today), $"{semester.Name} is now the active semester.");
    }

    private async Task DeactivateAllAsync(CancellationToken ct)
    {
        foreach (var s in await _db.Semesters.Where(s => s.IsActive).ToListAsync(ct))
            s.IsActive = false;
    }
}
