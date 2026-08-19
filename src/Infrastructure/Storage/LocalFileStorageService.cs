using EventHub.Application.Interfaces.Storage;
using Microsoft.Extensions.Options;

namespace Infrastructure.Storage;

public class LocalFileStorageService(IOptions<FileStorageOptions> options) : IFileStorageService
{
    private readonly string _uploadRoot = options.Value.UploadPath;

    public async Task<(string StoredFileName, string FilePath, string ContentType, long FileSizeInBytes)> SaveAsync(
        byte[] content,
        string originalFileName,
        string contentType,
        CancellationToken cancellationToken = default)
    {
        if (!Directory.Exists(_uploadRoot))
            Directory.CreateDirectory(_uploadRoot);

        var filePath = Path.Combine(_uploadRoot, originalFileName);

        await File.WriteAllBytesAsync(filePath, content, cancellationToken);

        return (originalFileName, filePath, contentType, content.Length);
    }

    public async Task<(byte[] Content, string ContentType)> ReadAsync(
        string fileName,
        CancellationToken cancellationToken = default)
    {
        var filePath = Path.Combine(_uploadRoot, fileName);
        var content = await File.ReadAllBytesAsync(filePath, cancellationToken);
        var contentType = GetContentType(fileName);
        return (content, contentType);
    }

    private static string GetContentType(string fileName)
    {
        var ext = Path.GetExtension(fileName).ToLowerInvariant();
        return ext switch
        {
            ".pdf" => "application/pdf",
            ".png" => "image/png",
            ".jpg" or ".jpeg" => "image/jpeg",
            _ => "application/octet-stream"
        };
    }
}
