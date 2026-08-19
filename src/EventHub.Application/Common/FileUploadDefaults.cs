namespace EventHub.Application.Common;

public static class FileUploadDefaults
{
    public static readonly string[] AllowedExtensions = [".png", ".jpg", ".jpeg", ".pdf"];
    public const long MaxFileSizeInBytes = 10 * 1024 * 1024;
}
