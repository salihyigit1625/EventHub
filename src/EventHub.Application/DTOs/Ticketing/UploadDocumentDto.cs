namespace EventHub.Application.DTOs.Ticketing;

public class UploadDocumentDto
{
    public string OriginalFileName { get; set; } = string.Empty;
    public byte[] Content { get; set; } = [];
}
