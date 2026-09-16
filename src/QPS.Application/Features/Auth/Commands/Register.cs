using FluentValidation;
using MediatR;
using QPS.Application.Common.Interfaces;
using QPS.Application.Common.Models;
using QPS.Application.Features.Auth.DTOs;
using QPS.Domain.Entities;
using QPS.Domain.Enums;

namespace QPS.Application.Features.Auth.Commands;

public class RegisterValidator : AbstractValidator<RegisterCommand>
{
    public RegisterValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().EmailAddress().MaximumLength(256);
        RuleFor(x => x.Password)
            .NotEmpty().MinimumLength(6).MaximumLength(100);
        RuleFor(x => x.FirstName)
            .NotEmpty().MaximumLength(100);
        RuleFor(x => x.LastName)
            .NotEmpty().MaximumLength(100);
    }
}

public class RegisterHandler : IRequestHandler<RegisterCommand, Result<AuthResponseDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ITokenService _tokenService;

    public RegisterHandler(IApplicationDbContext context, ITokenService tokenService)
    {
        _context = context;
        _tokenService = tokenService;
    }

    public async Task<Result<AuthResponseDto>> Handle(
        RegisterCommand request, CancellationToken cancellationToken)
    {
        if (_context.Users.Any(u => u.Email == request.Email))
            return Result<AuthResponseDto>.Fail("Email already exists");

        var user = new User
        {
            Email = request.Email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            FirstName = request.FirstName,
            LastName = request.LastName,
            Role = UserRole.TA,
            IsActive = true
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync(cancellationToken);

        var token = _tokenService.GenerateAccessToken(user);
        var refreshToken = _tokenService.GenerateRefreshToken();

        var response = new AuthResponseDto(
            token,
            refreshToken,
            DateTime.UtcNow.AddMinutes(60));

        return Result<AuthResponseDto>.Ok(response, "Registration successful");
    }
}