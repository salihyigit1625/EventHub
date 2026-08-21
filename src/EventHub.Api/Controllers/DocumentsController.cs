using EventHub.Api.Authorization;
using EventHub.Application.Common;
using EventHub.Application.DTOs.Ticketing;
using EventHub.Application.Interfaces.Ticketing;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace EventHub.Api.Controllers;

[ApiController]
[Route("api/documents")]
public class DocumentsController(IDocumentService documentService) : ControllerBase
{
    [HttpGet]
    [Authorize(Roles = $"{AppRoles.Organizer},{AppRoles.Admin}")]
    [HasPermission(AppPermissions.DocumentsDownload)]
    [EnableRateLimiting("documents-relaxed")]
    public async Task<ActionResult<IReadOnlyList<DocumentDto>>> GetMine(CancellationToken cancellationToken)
    {
        var result = await documentService.GetMyDocumentsAsync(cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id:int}")]
    [Authorize(Roles = $"{AppRoles.Organizer},{AppRoles.Admin}")]
    [HasPermission(AppPermissions.DocumentsDownload)]
    [EnableRateLimiting("documents-relaxed")]
    public async Task<IActionResult> Download(int id, CancellationToken cancellationToken)
    {
        var (content, contentType) = await documentService.DownloadAsync(id, cancellationToken);
        return File(content, contentType);
    }

    [HttpPost]
    [Consumes("multipart/form-data")]
    [Authorize(Roles = $"{AppRoles.Organizer},{AppRoles.Admin}")]
    [HasPermission(AppPermissions.DocumentsUpload)]
    [EnableRateLimiting("documents-strict")]
    [RequestSizeLimit(FileUploadDefaults.MaxFileSizeInBytes)]
    [RequestFormLimits(MultipartBodyLengthLimit = FileUploadDefaults.MaxFileSizeInBytes)]
    public async Task<ActionResult<DocumentDto>> Upload(IFormFile file, CancellationToken cancellationToken)
    {
        if (file is null || file.Length == 0)
            return BadRequest(new { title = "Validation failed", status = 400, detail = "Unable to upload document." });

        if (file.Length > FileUploadDefaults.MaxFileSizeInBytes)
            return BadRequest(new { title = "Validation failed", status = 400, detail = "Unable to upload document." });

        try
        {
            DocumentFileValidation.GetSafeExtension(file.FileName);
        }
        catch (InvalidOperationException)
        {
            return BadRequest(new { title = "Validation failed", status = 400, detail = "Unable to upload document." });
        }

        await using var stream = new MemoryStream((int)file.Length);
        await file.CopyToAsync(stream, cancellationToken);

        var dto = new UploadDocumentDto
        {
            OriginalFileName = file.FileName,
            Content = stream.ToArray()
        };

        var result = await documentService.UploadAsync(dto, cancellationToken);
        return Ok(result);
    }
}
