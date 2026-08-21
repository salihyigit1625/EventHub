using EventHub.Application.DTOs.Ticketing;

namespace EventHub.Application.Interfaces.Ticketing;

public interface IDocumentService
{
    Task<DocumentDto> UploadAsync(UploadDocumentDto dto, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<DocumentDto>> GetMyDocumentsAsync(CancellationToken cancellationToken = default);
    Task<(byte[] Content, string ContentType)> DownloadAsync(int documentId, CancellationToken cancellationToken = default);
}
