using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QPS.Application.Features.Auth.Commands;
using QPS.Application.Features.Auth.DTOs;
using QPS.Application.Features.Auth.Queries;

namespace QPS.API.Controllers;

public class AuthController : ApiControllerBase
{
    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<ActionResult> Login(LoginCommand command, CancellationToken ct)
    {
        var result = await Sender.Send(command, ct);
        return result.IsSuccess ? Ok(result) : Unauthorized(result);
    }

    [Authorize]
    [HttpGet("me")]
    public async Task<ActionResult<CurrentUserDto>> Me(CancellationToken ct) =>
        Ok(await Sender.Send(new GetCurrentUserQuery(), ct));

    [Authorize]
    [HttpPost("change-password")]
    public async Task<ActionResult> ChangePassword(ChangePasswordCommand command, CancellationToken ct) =>
        FromResult(await Sender.Send(command, ct));
}
