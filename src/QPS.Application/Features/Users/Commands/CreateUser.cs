using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using QPS.Application.Common.Interfaces;
using QPS.Application.Common.Models;
using QPS.Application.Features.Users.DTOs;
using QPS.Domain.Entities;
using QPS.Domain.Enums;

namespace QPS.Application.Features.Users.Commands;

public class CreateUserValidator : AbstractValidator<CreateUserCommand>
{
    private readonly IApplicationDbContext _context;

    public CreateUserValidator(IApplicationDbContext context)
    {
        _context = context;
        RuleFor(x => x.Email).NotEmpty().EmailAddress()
            .MustAsync(BeUniqueEmail).WithMessage("Email already exists");
        RuleFor(x => x.Password).NotEmpty().MinimumLength(6);
        RuleFor(x => x.FirstName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.LastName).NotEmpty().MaximumLength(100);
    }

    private async Task<bool> BeUniqueEmail(string email, CancellationToken ct)
        => !await _context.Users.AnyAsync(u => u.Email == email, ct);
}

public class CreateUserHandler : IRequestHandler<CreateUserCommand, Result<UserDto>>
{
    private readonly IApplicationDbContext _context;

    public CreateUserHandler(IApplicationDbContext context) => _context = context;

    public async Task<Result<UserDto>> Handle(
        CreateUserCommand request, CancellationToken cancellationToken)
    {
        var user = new User
        {
            Email = request.Email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            FirstName = request.FirstName,
            LastName = request.LastName,
            Role = request.Role,
            DayOff = request.DayOff,
            TargetWorkload = request.TargetWorkload,
            MaxProctoringSessionsPerSemester = request.MaxProctoringSessionsPerSemester,
            IsActive = true
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync(cancellationToken);

        var dto = new UserDto(
            user.Id, user.Email, user.FirstName, user.LastName, user.FullName,
            user.Role, user.DayOff, user.TargetWorkload,
            user.MaxProctoringSessionsPerSemester, user.IsActive, 0);

        return Result<UserDto>.Ok(dto, "User created successfully");
    }
}
