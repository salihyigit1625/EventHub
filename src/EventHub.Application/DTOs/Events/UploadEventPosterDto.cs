namespace EventHub.Application.DTOs.Events;

public class UploadEventPosterDto
{
    public int EventId { get; set; }
    public string OriginalFileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public byte[] Content { get; set; } = [];
}
