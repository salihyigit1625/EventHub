namespace EventHub.Application.Common;

public static class PosterFileValidation
{
    public static readonly string[] AllowedExtensions = [".png", ".jpg", ".jpeg"];

    public static string GetSafeExtension(string fileName)
    {
        var safeName = Path.GetFileName(fileName);
        if (string.IsNullOrWhiteSpace(safeName))
            throw new InvalidOperationException("Invalid file name.");

        var extension = Path.GetExtension(safeName).ToLowerInvariant();
        if (!AllowedExtensions.Contains(extension))
            throw new InvalidOperationException("Only .png and .jpg files are allowed for event posters.");

        return extension;
    }

    public static void EnsureMagicBytesMatch(byte[] content, string extension)
    {
        if (content.Length < 4)
            throw new InvalidOperationException("File content is invalid.");

        var isValid = extension switch
        {
            ".png" => content.Length >= 8
                      && content[0] == 0x89
                      && content[1] == 0x50
                      && content[2] == 0x4E
                      && content[3] == 0x47,
            ".jpg" or ".jpeg" => content[0] == 0xFF && content[1] == 0xD8 && content[2] == 0xFF,
            _ => false
        };

        if (!isValid)
            throw new InvalidOperationException("File content does not match the declared image type.");
    }

    public static string ContentTypeForExtension(string extension) => extension switch
    {
        ".png" => "image/png",
        ".jpg" or ".jpeg" => "image/jpeg",
        _ => "application/octet-stream"
    };
}
