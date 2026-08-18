using EventHub.Domain.Common;
using EventHub.Domain.Entities.Events;
using EventHub.Domain.Entities.Identity;

namespace EventHub.Domain.Entities.Ticketing;

public class Document : BaseEntity
{
    public int UploadedByUserId { get; set; }
    public virtual User UploadedByUser { get; set; } = null!;

    public int? EventId { get; set; }
    public virtual Event? Event { get; set; }

    public string OriginalFileName { get; set; } = string.Empty;
    public string StoredFileName { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long FileSizeInBytes { get; set; }
    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
}