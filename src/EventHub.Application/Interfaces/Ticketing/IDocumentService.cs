using EventHub.Application.DTOs.Ticketing;

namespace EventHub.Application.Interfaces.Ticketing;

public interface IDocumentService
{
    Task<DocumentDto> UploadAsync(UploadDocumentDto dto, CancellationToken cancellationToken = default);
    Task<(byte[] Content, string ContentType, string FileName)> DownloadAsync(int documentId, CancellationToken cancellationToken = default);
}
