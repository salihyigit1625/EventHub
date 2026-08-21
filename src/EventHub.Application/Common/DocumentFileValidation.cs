namespace EventHub.Application.Common;

public static class DocumentFileValidation
{
    public static readonly string[] AllowedExtensions = [".png", ".jpg", ".jpeg", ".pdf"];

    public static string GetSafeExtension(string fileName)
    {
        var safeName = Path.GetFileName(fileName);
        if (string.IsNullOrWhiteSpace(safeName))
            throw new InvalidOperationException("Unable to upload document.");

        var extension = Path.GetExtension(safeName).ToLowerInvariant();
        if (!AllowedExtensions.Contains(extension))
            throw new InvalidOperationException("Unable to upload document.");

        return extension;
    }

    public static void EnsureMagicBytesMatch(byte[] content, string extension)
    {
        if (content.Length < 4)
            throw new InvalidOperationException("Unable to upload document.");

        var isValid = extension switch
        {
            ".png" => content.Length >= 8
                      && content[0] == 0x89
                      && content[1] == 0x50
                      && content[2] == 0x4E
                      && content[3] == 0x47,
            ".jpg" or ".jpeg" => content[0] == 0xFF && content[1] == 0xD8 && content[2] == 0xFF,
            ".pdf" => content[0] == (byte)'%'
                      && content[1] == (byte)'P'
                      && content[2] == (byte)'D'
                      && content[3] == (byte)'F',
            _ => false
        };

        if (!isValid)
            throw new InvalidOperationException("Unable to upload document.");
    }

    public static string ContentTypeForExtension(string extension) => extension switch
    {
        ".png" => "image/png",
        ".jpg" or ".jpeg" => "image/jpeg",
        ".pdf" => "application/pdf",
        _ => "application/octet-stream"
    };
}
