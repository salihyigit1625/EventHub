using EventHub.Application.Interfaces.Storage;
using Microsoft.Extensions.Options;

namespace EventHub.Infrastructure.Storage;

public class LocalFileStorageService(IOptions<FileStorageOptions> options) : IFileStorageService
{
    private readonly string _uploadRoot = Path.GetFullPath(options.Value.UploadPath);

    public async Task<(string StoredFileName, string FilePath, string ContentType, long FileSizeInBytes)> SaveAsync(
        byte[] content,
        string originalFileName,
        string contentType,
        CancellationToken cancellationToken = default)
    {
        if (!Directory.Exists(_uploadRoot))
            Directory.CreateDirectory(_uploadRoot);

        var safeName = Path.GetFileName(originalFileName);
        if (string.IsNullOrWhiteSpace(safeName))
            throw new InvalidOperationException("Invalid file name.");

        var filePath = ResolvePathInsideRoot(safeName);

        await File.WriteAllBytesAsync(filePath, content, cancellationToken);

        return (safeName, filePath, contentType, content.Length);
    }

    public async Task<(byte[] Content, string ContentType)> ReadAsync(
        string fileName,
        CancellationToken cancellationToken = default)
    {
        var filePath = ResolvePathInsideRoot(fileName);
        var content = await File.ReadAllBytesAsync(filePath, cancellationToken);
        var contentType = GetContentType(fileName);
        return (content, contentType);
    }

    public Task DeleteAsync(string fileName, CancellationToken cancellationToken = default)
    {
        var filePath = ResolvePathInsideRoot(fileName);
        if (File.Exists(filePath))
            File.Delete(filePath);

        return Task.CompletedTask;
    }

    private string ResolvePathInsideRoot(string fileName)
    {
        var safeName = Path.GetFileName(fileName);
        if (string.IsNullOrWhiteSpace(safeName))
            throw new InvalidOperationException("Invalid file name.");

        var fullPath = Path.GetFullPath(Path.Combine(_uploadRoot, safeName));
        var rootWithSeparator = _uploadRoot.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
                                 + Path.DirectorySeparatorChar;

        if (!fullPath.StartsWith(rootWithSeparator, StringComparison.OrdinalIgnoreCase)
            && !string.Equals(fullPath, _uploadRoot, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Invalid file path.");

        return fullPath;
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
