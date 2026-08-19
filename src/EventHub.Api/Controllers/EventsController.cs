using EventHub.Api.Authorization;
using EventHub.Application.Common;
using EventHub.Application.DTOs.Events;
using EventHub.Application.Interfaces.Events;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EventHub.Api.Controllers;

[ApiController]
[Route("api/events")]
public class EventsController(IEventService eventService) : ControllerBase
{
    [HttpGet]
    [AllowAnonymous]
    public async Task<ActionResult<IReadOnlyList<EventListItemDto>>> GetPublished(CancellationToken cancellationToken)
    {
        var result = await eventService.GetPublishedAsync(cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id:int}")]
    [AllowAnonymous]
    public async Task<ActionResult<EventDto>> GetById(int id, CancellationToken cancellationToken)
    {
        var result = await eventService.GetByIdAsync(id, cancellationToken);
        return Ok(result);
    }

    [HttpGet("mine")]
    [Authorize(Roles = AppRoles.Organizer)]
    [HasPermission(AppPermissions.EventsCreate)]
    public async Task<ActionResult<IReadOnlyList<EventListItemDto>>> GetMine(CancellationToken cancellationToken)
    {
        var result = await eventService.GetMyEventsAsync(cancellationToken);
        return Ok(result);
    }

    [HttpPost]
    [Authorize(Roles = AppRoles.Organizer)]
    [HasPermission(AppPermissions.EventsCreate)]
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
    public async Task<ActionResult<EventDto>> Publish(int id, CancellationToken cancellationToken)
    {
        var result = await eventService.PublishAsync(id, cancellationToken);
        return Ok(result);
    }

    [HttpPost("{id:int}/cancel")]
    [Authorize(Roles = AppRoles.Organizer)]
    [HasPermission(AppPermissions.EventsCancel)]
    public async Task<ActionResult<EventDto>> Cancel(int id, CancellationToken cancellationToken)
    {
        var result = await eventService.CancelAsync(id, cancellationToken);
        return Ok(result);
    }

    [HttpPost("{id:int}/poster")]
    [Consumes("multipart/form-data")]
    [Authorize(Roles = AppRoles.Organizer)]
    [HasPermission(AppPermissions.EventsPosterUpload)]
    public async Task<ActionResult<EventDto>> UploadPoster(
        int id,
        IFormFile file,
        CancellationToken cancellationToken)
    {
        await using var stream = new MemoryStream();
        await file.CopyToAsync(stream, cancellationToken);

        var dto = new UploadEventPosterDto
        {
            EventId = id,
            OriginalFileName = file.FileName,
            ContentType = file.ContentType,
            Content = stream.ToArray()
        };

        var result = await eventService.UploadPosterAsync(dto, cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id:int}/poster")]
    [AllowAnonymous]
    public async Task<IActionResult> GetPoster(int id, CancellationToken cancellationToken)
    {
        var (content, contentType, fileName) = await eventService.GetPosterAsync(id, cancellationToken);
        return File(content, contentType, fileName);
    }
}
