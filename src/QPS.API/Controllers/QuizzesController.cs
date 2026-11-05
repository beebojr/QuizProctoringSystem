using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QPS.Application.Features.Quizzes.Commands;
using QPS.Application.Features.Quizzes.DTOs;
using QPS.Application.Features.Quizzes.Import;
using QPS.Application.Features.Quizzes.Queries;
using QPS.Domain.Enums;

namespace QPS.API.Controllers;

[Authorize(Roles = "Admin")]
public class QuizzesController : ApiControllerBase
{
    [HttpGet]
    public async Task<ActionResult<List<QuizDto>>> Get(
        [FromQuery] Guid? semesterId, [FromQuery] QuizStatus? status, [FromQuery] string? search,
        [FromQuery] bool incompleteOnly = false, [FromQuery] DateOnly? from = null, [FromQuery] DateOnly? to = null,
        [FromQuery] Guid? courseId = null, CancellationToken ct = default) =>
        Ok(await Sender.Send(new GetQuizzesQuery(semesterId, status, search, incompleteOnly, from, to, courseId), ct));

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<QuizDto>> GetOne(Guid id, CancellationToken ct) =>
        Ok(await Sender.Send(new GetQuizQuery(id), ct));

    [HttpPost]
    public async Task<ActionResult> Create(CreateQuizCommand command, CancellationToken ct) =>
        FromResult(await Sender.Send(command, ct));

    [HttpPut("{id:guid}")]
    public async Task<ActionResult> Update(Guid id, UpdateQuizCommand command, CancellationToken ct) =>
        FromResult(await Sender.Send(command with { Id = id }, ct));

    [HttpDelete("{id:guid}")]
    public async Task<ActionResult> Delete(Guid id, CancellationToken ct) =>
        FromResult(await Sender.Send(new DeleteQuizCommand(id), ct));

    /// <summary>Upload the university quiz calendar (.xlsx) and see what would be imported.</summary>
    [HttpPost("import/preview")]
    [RequestSizeLimit(16 * 1024 * 1024)]
    public async Task<ActionResult> PreviewExcel(IFormFile file, [FromForm] Guid? semesterId, CancellationToken ct)
    {
        await using var stream = await ReadExcelAsync(file, ct)
                                 ?? throw new BadHttpRequestException("Choose a file to upload.");
        return FromResult(await Sender.Send(new ImportPreviewCommand(semesterId, stream, null), ct));
    }

    public record PreviewRowsRequest(Guid? SemesterId, List<RawRowInput> Rows);

    /// <summary>Rows extracted in the browser from a PDF calendar.</summary>
    [HttpPost("import/preview-rows")]
    public async Task<ActionResult> PreviewRows(PreviewRowsRequest body, CancellationToken ct) =>
        FromResult(await Sender.Send(new ImportPreviewCommand(body.SemesterId, null, body.Rows), ct));

    [HttpPost("import/commit")]
    public async Task<ActionResult> Commit(ImportCommitCommand command, CancellationToken ct) =>
        FromResult(await Sender.Send(command, ct));

    [HttpGet("import/template")]
    public async Task<FileContentResult> Template(CancellationToken ct) =>
        Download(await Sender.Send(new GetImportTemplateQuery(), ct));

    [HttpGet("export")]
    public async Task<FileContentResult> Export([FromQuery] Guid? semesterId, CancellationToken ct) =>
        Download(await Sender.Send(new ExportQuizzesQuery(semesterId), ct));
}
