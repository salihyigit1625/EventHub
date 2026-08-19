using EventHub.Application.Interfaces.Storage;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EventHub.Api.Controllers;

[ApiController]
[Route("api/files")]
public class FilesController(IFileStorageService fileStorage) : ControllerBase
{
    [HttpGet("serve")]
    [AllowAnonymous]
    public async Task<IActionResult> Serve([FromQuery] string fileName, CancellationToken cancellationToken)
    {
        var (content, contentType) = await fileStorage.ReadAsync(fileName, cancellationToken);
        return File(content, contentType, fileName);
    }
}
