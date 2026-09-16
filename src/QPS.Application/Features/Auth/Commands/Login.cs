using FluentValidation;
using MediatR;
using QPS.Application.Common.Interfaces;
using QPS.Application.Common.Models;
using QPS.Application.Features.Auth.DTOs;

namespace QPS.Application.Features.Auth.Commands;

public class LoginValidator : AbstractValidator<LoginCommand>
{
    public LoginValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().EmailAddress();
        RuleFor(x => x.Password)
            .NotEmpty();
    }
}

public class LoginHandler : IRequestHandler<LoginCommand, Result<AuthResponseDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ITokenService _tokenService;

    public LoginHandler(IApplicationDbContext context, ITokenService tokenService)
    {
        _context = context;
        _tokenService = tokenService;
    }

    public Task<Result<AuthResponseDto>> Handle(
        LoginCommand request, CancellationToken cancellationToken)
    {
        var user = _context.Users.FirstOrDefault(u => u.Email == request.Email);
        if (user == null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            return Task.FromResult(Result<AuthResponseDto>.Fail("Invalid email or password"));

        if (!user.IsActive)
            return Task.FromResult(Result<AuthResponseDto>.Fail("Account is inactive"));

        var token = _tokenService.GenerateAccessToken(user);
        var refreshToken = _tokenService.GenerateRefreshToken();

        var response = new AuthResponseDto(
            token,
            refreshToken,
            DateTime.UtcNow.AddMinutes(60));

        return Task.FromResult(Result<AuthResponseDto>.Ok(response, "Login successful"));
    }
}