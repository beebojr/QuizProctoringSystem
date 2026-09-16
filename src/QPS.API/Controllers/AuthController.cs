using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QPS.Application.Common.Models;
using QPS.Application.Features.Auth.Commands;
using QPS.Application.Features.Auth.DTOs;
using QPS.Application.Features.Auth.Queries;

namespace QPS.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly ISender _sender;

    public AuthController(ISender sender) => _sender = sender;

    [HttpPost("register")]
    public async Task<ActionResult<Result<AuthResponseDto>>> Register(
        RegisterCommand command, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(command, cancellationToken);
        if (!result.IsSuccess)
            return BadRequest(result);
        return Ok(result);
    }

    [HttpPost("login")]
    public async Task<ActionResult<Result<AuthResponseDto>>> Login(
        LoginCommand command, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(command, cancellationToken);
        if (!result.IsSuccess)
            return Unauthorized(result);
        return Ok(result);
    }

    [Authorize]
    [HttpGet("me")]
    public async Task<ActionResult<Result<CurrentUserDto>>> GetCurrentUser(
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new GetCurrentUserQuery(), cancellationToken);
        if (!result.IsSuccess)
            return NotFound(result);
        return Ok(result);
    }
}