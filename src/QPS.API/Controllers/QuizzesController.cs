using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QPS.Application.Common.Models;
using QPS.Application.Features.Quizzes.Commands;
using QPS.Application.Features.Quizzes.DTOs;
using QPS.Application.Features.Quizzes.Queries;
using QPS.Domain.Enums;

namespace QPS.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class QuizzesController : ControllerBase
{
    private readonly ISender _sender;

    public QuizzesController(ISender sender) => _sender = sender;

    [HttpGet]
    public async Task<ActionResult<List<QuizDto>>> GetQuizzes(
        [FromQuery] Guid? semesterId = null,
        [FromQuery] QuizStatus? status = null,
        CancellationToken cancellationToken = default)
    {
        var result = await _sender.Send(new GetQuizzesQuery(semesterId, status), cancellationToken);
        return Ok(result);
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<Result<QuizDto>>> CreateQuiz(
        CreateQuizCommand command, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(command, cancellationToken);
        if (!result.IsSuccess) return BadRequest(result);
        return Ok(result);
    }

    [HttpPost("import")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<Result<List<QuizDto>>>> ImportQuizzes(
        [FromForm] ImportQuizzesCommand command, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(command, cancellationToken);
        if (!result.IsSuccess) return BadRequest(result);
        return Ok(result);
    }
}