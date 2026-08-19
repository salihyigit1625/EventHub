namespace EventHub.Application.DTOs.Ticketing;

public class DocumentDto
{
    public int Id { get; set; }
    public int UploadedByUserId { get; set; }
    public string OriginalFileName { get; set; } = string.Empty;
    public string StoredFileName { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long FileSizeInBytes { get; set; }
    public DateTime CreatedAt { get; set; }
}
