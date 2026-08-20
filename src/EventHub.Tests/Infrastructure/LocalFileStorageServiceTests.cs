using EventHub.Infrastructure.Storage;
using Microsoft.Extensions.Options;

namespace EventHub.Tests.Infrastructure;

[TestFixture]
public class LocalFileStorageServiceTests
{
    private string _root = null!;
    private LocalFileStorageService _storage = null!;

    [SetUp]
    public void SetUp()
    {
        _root = Path.Combine(Path.GetTempPath(), "eventhub-tests", Guid.NewGuid().ToString("N"));
        _storage = new LocalFileStorageService(Options.Create(new FileStorageOptions { UploadPath = _root }));
    }

    [TearDown]
    public void TearDown()
    {
        if (Directory.Exists(_root))
            Directory.Delete(_root, recursive: true);
    }

    [Test]
    public async Task Save_UsesFileNameOnly()
    {
        var saved = await _storage.SaveAsync([1, 2, 3], "poster.png", "image/png");

        Assert.That(saved.StoredFileName, Is.EqualTo("poster.png"));
        Assert.That(File.Exists(Path.Combine(_root, "poster.png")), Is.True);
        Assert.That(saved.FileSizeInBytes, Is.EqualTo(3));
    }

    [Test]
    public async Task Save_StripsDirectorySegments()
    {
        var saved = await _storage.SaveAsync([1], "sub/../../evil.png", "image/png");

        Assert.That(saved.StoredFileName, Is.EqualTo("evil.png"));
        Assert.That(File.Exists(Path.Combine(_root, "evil.png")), Is.True);
        Assert.That(File.Exists(Path.GetFullPath(Path.Combine(_root, "..", "evil.png"))), Is.False);
    }

    [Test]
    public async Task Save_OverwritesSameName()
    {
        await _storage.SaveAsync([1], "a.png", "image/png");
        await _storage.SaveAsync([9, 9], "a.png", "image/png");

        var bytes = await File.ReadAllBytesAsync(Path.Combine(_root, "a.png"));
        Assert.That(bytes, Is.EqualTo(new byte[] { 9, 9 }));
    }

    [Test]
    public async Task Read_ReturnsContentTypeByExtension()
    {
        await _storage.SaveAsync([4], "doc.PDF", "ignored");
        var read = await _storage.ReadAsync("doc.PDF");
        Assert.That(read.ContentType, Is.EqualTo("application/pdf"));
        Assert.That(read.Content, Is.EqualTo(new byte[] { 4 }));
    }

    [Test]
    public async Task Read_JpegAndPngContentTypes()
    {
        await _storage.SaveAsync([1], "a.jpg", "x");
        await _storage.SaveAsync([1], "b.png", "x");

        Assert.That((await _storage.ReadAsync("a.jpg")).ContentType, Is.EqualTo("image/jpeg"));
        Assert.That((await _storage.ReadAsync("b.png")).ContentType, Is.EqualTo("image/png"));
    }

    [Test]
    public async Task Read_UnknownExtension_IsOctetStream()
    {
        await _storage.SaveAsync([1], "a.bin", "x");
        Assert.That((await _storage.ReadAsync("a.bin")).ContentType, Is.EqualTo("application/octet-stream"));
    }

    [Test]
    public async Task Read_RelativeSegments_CannotEscapeRoot()
    {
        await _storage.SaveAsync([1], "dummy.png", "image/png");

        var outside = Path.GetFullPath(Path.Combine(_root, "..", "secret.txt"));
        await File.WriteAllBytesAsync(outside, [42]);
        try
        {
            Assert.ThrowsAsync<FileNotFoundException>(async () =>
                await _storage.ReadAsync(Path.Combine("..", "secret.txt")));
        }
        finally
        {
            if (File.Exists(outside))
                File.Delete(outside);
        }
    }

    [Test]
    public async Task Delete_RemovesFile()
    {
        await _storage.SaveAsync([1], "gone.png", "image/png");
        await _storage.DeleteAsync("gone.png");
        Assert.That(File.Exists(Path.Combine(_root, "gone.png")), Is.False);
    }
}
