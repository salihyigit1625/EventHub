using AutoMapper;
using EventHub.Application.Common;
using EventHub.Application.DTOs.Ticketing;
using EventHub.Application.Interfaces.Identity;
using EventHub.Application.Interfaces.Persistence;
using EventHub.Application.Interfaces.Storage;
using EventHub.Application.Interfaces.Ticketing;
using EventHub.Domain.Entities.Ticketing;

namespace EventHub.Application.Services.Ticketing;

public class DocumentService(
    IGenericRepository<Document> documentRepository,
    IUnitOfWork unitOfWork,
    ICurrentUserService currentUser,
    IFileStorageService fileStorage,
    IMapper mapper) : IDocumentService
{
    public async Task<DocumentDto> UploadAsync(
        UploadDocumentDto dto,
        CancellationToken cancellationToken = default)
    {
        var userId = currentUser.UserId
            ?? throw new UnauthorizedAccessException("Authentication is required.");

        var extension = DocumentFileValidation.GetSafeExtension(dto.OriginalFileName);
        DocumentFileValidation.EnsureMagicBytesMatch(dto.Content, extension);

        var storedFileName = $"{Guid.NewGuid():N}{extension}";
        var contentType = DocumentFileValidation.ContentTypeForExtension(extension);

        var stored = await fileStorage.SaveAsync(
            dto.Content,
            storedFileName,
            contentType,
            cancellationToken);

        var document = new Document
        {
            UploadedByUserId = userId,
            OriginalFileName = Path.GetFileName(dto.OriginalFileName),
            StoredFileName = stored.StoredFileName,
            FilePath = stored.FilePath,
            ContentType = stored.ContentType,
            FileSizeInBytes = stored.FileSizeInBytes
        };

        await documentRepository.AddAsync(document, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return mapper.Map<DocumentDto>(document);
    }

    public async Task<IReadOnlyList<DocumentDto>> GetMyDocumentsAsync(
        CancellationToken cancellationToken = default)
    {
        var userId = currentUser.UserId
            ?? throw new UnauthorizedAccessException("Authentication is required.");

        var documents = await documentRepository.FindAsync(
            d => d.UploadedByUserId == userId,
            cancellationToken);

        return documents
            .OrderByDescending(d => d.CreatedAt)
            .Select(d => mapper.Map<DocumentDto>(d))
            .ToList();
    }

    public async Task<(byte[] Content, string ContentType)> DownloadAsync(
        int documentId,
        CancellationToken cancellationToken = default)
    {
        var userId = currentUser.UserId
            ?? throw new UnauthorizedAccessException("Authentication is required.");

        var document = await documentRepository.GetByIdAsync(documentId, cancellationToken)
            ?? throw new KeyNotFoundException($"Document ({documentId}) was not found.");

        if (document.UploadedByUserId != userId)
            throw new KeyNotFoundException($"Document ({documentId}) was not found.");

        return await fileStorage.ReadAsync(document.StoredFileName, cancellationToken);
    }
}
