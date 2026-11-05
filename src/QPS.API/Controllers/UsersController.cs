using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QPS.Application.Features.Users.Commands;
using QPS.Application.Features.Users.DTOs;
using QPS.Application.Features.Users.Queries;
using QPS.Domain.Enums;

namespace QPS.API.Controllers;

[Authorize(Roles = "Admin")]
public class UsersController : ApiControllerBase
{
    [HttpGet]
    public async Task<ActionResult<List<UserDto>>> GetUsers([FromQuery] UserRole? role, [FromQuery] string? search,
        [FromQuery] bool includeInactive = true, CancellationToken ct = default) =>
        Ok(await Sender.Send(new GetUsersQuery(role, search, includeInactive), ct));

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<UserDto>> GetUser(Guid id, CancellationToken ct) =>
        Ok(await Sender.Send(new GetUserQuery(id), ct));

    [HttpPost]
    public async Task<ActionResult> Create(CreateUserCommand command, CancellationToken ct) =>
        FromResult(await Sender.Send(command, ct));

    [HttpPut("{id:guid}")]
    public async Task<ActionResult> Update(Guid id, UpdateUserCommand command, CancellationToken ct) =>
        FromResult(await Sender.Send(command with { Id = id }, ct));

    [HttpDelete("{id:guid}")]
    public async Task<ActionResult> Delete(Guid id, CancellationToken ct) =>
        FromResult(await Sender.Send(new DeleteUserCommand(id), ct));

    public record ToggleRequest(bool Enabled);

    [HttpPut("{id:guid}/schedule-editing")]
    public async Task<ActionResult> SetScheduleEditing(Guid id, ToggleRequest body, CancellationToken ct) =>
        FromResult(await Sender.Send(new SetScheduleEditingCommand(id, body.Enabled), ct));

    [HttpPut("schedule-editing")]
    public async Task<ActionResult> SetScheduleEditingForAll(ToggleRequest body, CancellationToken ct) =>
        FromResult(await Sender.Send(new SetScheduleEditingCommand(null, body.Enabled), ct));

    public record WorkloadRequest(double? TargetWorkloadOverride, int? MaxSessionsOverride);

    [HttpPut("{id:guid}/workload")]
    public async Task<ActionResult> UpdateWorkload(Guid id, WorkloadRequest body, CancellationToken ct) =>
        FromResult(await Sender.Send(new UpdateWorkloadCommand(id, body.TargetWorkloadOverride, body.MaxSessionsOverride), ct));
}
