using EventHub.Api.Authorization;
using EventHub.Application.Common;
using EventHub.Application.DTOs.Events;
using EventHub.Application.Interfaces.Events;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace EventHub.Api.Controllers;

[ApiController]
[Route("api/events")]
public class EventsController(IEventService eventService) : ControllerBase
{
    [HttpGet]
    [AllowAnonymous]
    [EnableRateLimiting("events-relaxed")]
    public async Task<ActionResult<PagedResult<EventListItemDto>>> GetPublished(
        [FromQuery] EventListQuery query,
        CancellationToken cancellationToken)
    {
        var result = await eventService.GetPublishedAsync(query, cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id:int}")]
    [AllowAnonymous]
    [EnableRateLimiting("events-relaxed")]
    public async Task<ActionResult<EventDto>> GetById(int id, CancellationToken cancellationToken)
    {
        var result = await eventService.GetByIdAsync(id, cancellationToken);
        return Ok(result);
    }

    [HttpGet("mine")]
    [Authorize(Roles = AppRoles.Organizer)]
    [HasPermission(AppPermissions.EventsCreate)]
    [EnableRateLimiting("events-relaxed")]
    public async Task<ActionResult<PagedResult<EventListItemDto>>> GetMine(
        [FromQuery] EventListQuery query,
        CancellationToken cancellationToken)
    {
        var result = await eventService.GetMyEventsAsync(query, cancellationToken);
        return Ok(result);
    }

    [HttpPost]
    [Authorize(Roles = AppRoles.Organizer)]
    [HasPermission(AppPermissions.EventsCreate)]
    [EnableRateLimiting("events-strict")]
    public async Task<ActionResult<EventDto>> Create(
        [FromBody] CreateEventDto dto,
        CancellationToken cancellationToken)
    {
        var result = await eventService.CreateAsync(dto, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = AppRoles.Organizer)]
    [HasPermission(AppPermissions.EventsUpdate)]
    [EnableRateLimiting("events-strict")]
    public async Task<ActionResult<EventDto>> Update(
        int id,
        [FromBody] UpdateEventDto dto,
        CancellationToken cancellationToken)
    {
        var result = await eventService.UpdateAsync(id, dto, cancellationToken);
        return Ok(result);
    }

    [HttpPost("{id:int}/publish")]
    [Authorize(Roles = AppRoles.Organizer)]
    [HasPermission(AppPermissions.EventsPublish)]
    [EnableRateLimiting("events-strict")]
    public async Task<ActionResult<EventDto>> Publish(int id, CancellationToken cancellationToken)
    {
        var result = await eventService.PublishAsync(id, cancellationToken);
        return Ok(result);
    }

    [HttpPost("{id:int}/cancel")]
    [Authorize(Roles = AppRoles.Organizer)]
    [HasPermission(AppPermissions.EventsCancel)]
    [EnableRateLimiting("events-strict")]
    public async Task<ActionResult<EventDto>> Cancel(int id, CancellationToken cancellationToken)
    {
        var result = await eventService.CancelAsync(id, cancellationToken);
        return Ok(result);
    }

    [HttpPost("{id:int}/poster")]
    [Consumes("multipart/form-data")]
    [Authorize(Roles = AppRoles.Organizer)]
    [HasPermission(AppPermissions.EventsPosterUpload)]
    [EnableRateLimiting("events-poster")]
    [RequestSizeLimit(FileUploadDefaults.MaxFileSizeInBytes)]
    [RequestFormLimits(MultipartBodyLengthLimit = FileUploadDefaults.MaxFileSizeInBytes)]
    public async Task<ActionResult<EventDto>> UploadPoster(
        int id,
        IFormFile file,
        CancellationToken cancellationToken)
    {
        if (file is null || file.Length == 0)
            return BadRequest(new { title = "Validation failed", status = 400, detail = "File is required." });

        if (file.Length > FileUploadDefaults.MaxFileSizeInBytes)
            return BadRequest(new { title = "Validation failed", status = 400, detail = "File size must be 10 MB or less." });

        try
        {
            PosterFileValidation.GetSafeExtension(file.FileName);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { title = "Validation failed", status = 400, detail = ex.Message });
        }

        await using var stream = new MemoryStream((int)file.Length);
        await file.CopyToAsync(stream, cancellationToken);

        var dto = new UploadEventPosterDto
        {
            EventId = id,
            OriginalFileName = file.FileName,
            Content = stream.ToArray()
        };

        var result = await eventService.UploadPosterAsync(dto, cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id:int}/poster")]
    [AllowAnonymous]
    [EnableRateLimiting("events-relaxed")]
    public async Task<IActionResult> GetPoster(int id, CancellationToken cancellationToken)
    {
        var (content, contentType) = await eventService.GetPosterAsync(id, cancellationToken);
        return File(content, contentType);
    }
}
