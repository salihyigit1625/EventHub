namespace EventHub.Application.Interfaces.Storage;

public interface IFileStorageService
{
    Task<(string StoredFileName, string FilePath, string ContentType, long FileSizeInBytes)> SaveAsync(
        byte[] content,
        string originalFileName,
        string contentType,
        CancellationToken cancellationToken = default);

    Task<(byte[] Content, string ContentType)> ReadAsync(
        string fileName,
        CancellationToken cancellationToken = default);

    Task DeleteAsync(string fileName, CancellationToken cancellationToken = default);
}
