using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using QPS.Application.Common.Interfaces;
using QPS.Application.Common.Models;
using QPS.Application.Features.Auth.DTOs;
using QPS.Application.Features.Auth.Queries;

namespace QPS.Application.Features.Auth.Commands;

public record LoginCommand(string Email, string Password) : IRequest<Result<AuthResponseDto>>;

public class LoginValidator : AbstractValidator<LoginCommand>
{
    public LoginValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.Password).NotEmpty();
    }
}

public class LoginHandler : IRequestHandler<LoginCommand, Result<AuthResponseDto>>
{
    private readonly IApplicationDbContext _db;
    private readonly ITokenService _tokens;

    public LoginHandler(IApplicationDbContext db, ITokenService tokens)
    {
        _db = db;
        _tokens = tokens;
    }

    public async Task<Result<AuthResponseDto>> Handle(LoginCommand request, CancellationToken ct)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == email && !u.IsDeleted, ct);

        // Same message for unknown email and wrong password (no account enumeration).
        if (user is null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            return Result<AuthResponseDto>.Fail("Incorrect email or password.");

        if (!user.IsActive)
            return Result<AuthResponseDto>.Fail("This account is deactivated. Contact an administrator.");

        user.LastLoginAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);

        var (token, expiresAt) = _tokens.GenerateAccessToken(user);
        return Result<AuthResponseDto>.Ok(new AuthResponseDto(token, expiresAt, GetCurrentUserHandler.ToDto(user)));
    }
}
