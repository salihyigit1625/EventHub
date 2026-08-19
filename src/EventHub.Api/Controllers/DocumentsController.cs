using EventHub.Api.Authorization;
using EventHub.Application.Common;
using EventHub.Application.DTOs.Ticketing;
using EventHub.Application.Interfaces.Storage;
using EventHub.Application.Interfaces.Ticketing;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EventHub.Api.Controllers;

[ApiController]
[Route("api/documents")]
public class DocumentsController(
    IDocumentService documentService,
    IFileStorageService fileStorage) : ControllerBase
{
    [HttpPost]
    [Authorize(Roles = $"{AppRoles.Organizer},{AppRoles.Admin}")]
    [HasPermission(AppPermissions.DocumentsUpload)]
    public async Task<ActionResult<DocumentDto>> Upload(IFormFile file, CancellationToken cancellationToken)
    {
        await using var stream = new MemoryStream();
        await file.CopyToAsync(stream, cancellationToken);

        var dto = new UploadDocumentDto
        {
            OriginalFileName = file.FileName,
            ContentType = file.ContentType,
            Content = stream.ToArray()
        };

        var result = await documentService.UploadAsync(dto, cancellationToken);
        return Ok(result);
    }

    [HttpGet]
    [Authorize]
    [HasPermission(AppPermissions.DocumentsDownload)]
    public async Task<IActionResult> Download([FromQuery] string fileName, CancellationToken cancellationToken)
    {
        var (content, contentType) = await fileStorage.ReadAsync(fileName, cancellationToken);
        return File(content, contentType, fileName);
    }
}
