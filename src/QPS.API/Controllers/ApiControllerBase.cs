using MediatR;
using Microsoft.AspNetCore.Mvc;
using QPS.Application.Common.Models;
using QPS.Application.Features.Quizzes.Queries;

namespace QPS.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public abstract class ApiControllerBase : ControllerBase
{
    private ISender? _sender;

    protected ISender Sender => _sender ??= HttpContext.RequestServices.GetRequiredService<ISender>();

    protected ActionResult FromResult<T>(Result<T> result) => result.IsSuccess ? Ok(result) : BadRequest(result);

    protected ActionResult FromResult(Result result) => result.IsSuccess ? Ok(result) : BadRequest(result);

    protected FileContentResult Download(FileDto file) => File(file.Content, file.ContentType, file.FileName);

    /// <summary>Reads an uploaded Excel file into memory (ClosedXML needs a seekable stream).</summary>
    protected static async Task<MemoryStream?> ReadExcelAsync(IFormFile? file, CancellationToken ct)
    {
        if (file is null || file.Length == 0) return null;
        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (ext is not (".xlsx" or ".xlsm"))
            throw new BadHttpRequestException(ext == ".xls"
                ? "Old .xls files aren't supported. Open it in Excel and save it as .xlsx."
                : "Upload an Excel workbook (.xlsx).");
        if (file.Length > 15 * 1024 * 1024) throw new BadHttpRequestException("The file is larger than 15 MB.");
        var ms = new MemoryStream();
        await file.CopyToAsync(ms, ct);
        ms.Position = 0;
        return ms;
    }
}
