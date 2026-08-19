using AutoMapper;
using EventHub.Application.DTOs.Ticketing;
using EventHub.Application.Interfaces.Persistence;
using EventHub.Application.Interfaces.Identity;
using EventHub.Application.Interfaces.Events;
using EventHub.Application.Interfaces.Profiles;
using EventHub.Application.Interfaces.Ticketing;
using EventHub.Application.Interfaces.Admin;
using EventHub.Application.Interfaces.Storage;
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
        var extension = Path.GetExtension(dto.OriginalFileName).ToLowerInvariant();
        if (!Common.FileUploadDefaults.AllowedExtensions.Contains(extension))
            throw new InvalidOperationException("Only .png, .jpg and .pdf files are allowed.");

        var userId = currentUser.UserId
            ?? throw new UnauthorizedAccessException("Authentication is required.");

        var stored = await fileStorage.SaveAsync(
            dto.Content,
            dto.OriginalFileName,
            dto.ContentType,
            cancellationToken);

        var document = new Document
        {
            UploadedByUserId = userId,
            OriginalFileName = dto.OriginalFileName,
            StoredFileName = stored.StoredFileName,
            FilePath = stored.FilePath,
            ContentType = stored.ContentType,
            FileSizeInBytes = stored.FileSizeInBytes
        };

        await documentRepository.AddAsync(document, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return mapper.Map<DocumentDto>(document);
    }

    public async Task<(byte[] Content, string ContentType, string FileName)> DownloadAsync(
        int documentId,
        CancellationToken cancellationToken = default)
    {
        var document = await documentRepository.GetByIdAsync(documentId, cancellationToken)
            ?? throw new KeyNotFoundException($"Document ({documentId}) was not found.");

        var (content, contentType) = await fileStorage.ReadAsync(document.StoredFileName, cancellationToken);
        return (content, contentType, document.OriginalFileName);
    }
}
