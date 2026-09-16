using FluentValidation;
using MediatR;
using QPS.Application.Common.Interfaces;
using QPS.Application.Common.Models;
using QPS.Application.Features.Assignments.DTOs;
using QPS.Domain.Entities;
using QPS.Domain.Enums;

namespace QPS.Application.Features.Assignments.Commands;

public class ManualAssignValidator : AbstractValidator<ManualAssignCommand>
{
    public ManualAssignValidator()
    {
        RuleFor(x => x.QuizLocationId).NotEmpty();
        RuleFor(x => x.UserId).NotEmpty();
    }
}

public class ManualAssignHandler : IRequestHandler<ManualAssignCommand, Result<ProctorAssignment>>
{
    private readonly IApplicationDbContext _context;

    public ManualAssignHandler(IApplicationDbContext context) => _context = context;

    public async Task<Result<ProctorAssignment>> Handle(
        ManualAssignCommand request, CancellationToken cancellationToken)
    {
        var assignment = new ProctorAssignment
        {
            QuizLocationId = request.QuizLocationId,
            UserId = request.UserId,
            IsBackup = request.IsBackup,
            Status = AssignmentStatus.Assigned
        };

        _context.ProctorAssignments.Add(assignment);
        await _context.SaveChangesAsync(cancellationToken);

        return Result<ProctorAssignment>.Ok(assignment, "Proctor assigned successfully");
    }
}