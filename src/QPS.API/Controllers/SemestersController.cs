using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QPS.Application.Features.Semesters.Commands;
using QPS.Application.Features.Semesters.DTOs;
using QPS.Application.Features.Semesters.Queries;

namespace QPS.API.Controllers;

[Authorize]
public class SemestersController : ApiControllerBase
{
    [HttpGet]
    public async Task<ActionResult<List<SemesterDto>>> Get(CancellationToken ct) =>
        Ok(await Sender.Send(new GetSemestersQuery(), ct));

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult> Create(CreateSemesterCommand command, CancellationToken ct) =>
        FromResult(await Sender.Send(command, ct));

    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult> Update(Guid id, UpdateSemesterCommand command, CancellationToken ct) =>
        FromResult(await Sender.Send(command with { Id = id }, ct));

    [HttpPost("{id:guid}/activate")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult> Activate(Guid id, CancellationToken ct) =>
        FromResult(await Sender.Send(new ActivateSemesterCommand(id), ct));
}
