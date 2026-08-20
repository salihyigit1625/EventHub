using EventHub.Application.DTOs.Ticketing;
using EventHub.Domain.Entities.Ticketing;
using EventHub.Tests.Fakes;

namespace EventHub.Tests.Services;

[TestFixture]
public class DocumentServiceTests
{
    private TestDb _db = null!;

    [SetUp]
    public void SetUp() => _db = new TestDb();

    [Test]
    public async Task Upload_StoresOriginalFileName()
    {
        _db.CurrentUser.UserId = 10;

        var dto = await _db.CreateDocumentService().UploadAsync(new UploadDocumentDto
        {
            OriginalFileName = "notes.pdf",
            ContentType = "application/pdf",
            Content = [1, 2, 3, 4]
        });

        Assert.That(dto.StoredFileName, Is.EqualTo("notes.pdf"));
        Assert.That(dto.FileSizeInBytes, Is.EqualTo(4));
        Assert.That(dto.UploadedByUserId, Is.EqualTo(10));
    }

    [Test]
    public void Upload_DisallowedExtension_Throws()
    {
        _db.CurrentUser.UserId = 10;
        Assert.ThrowsAsync<InvalidOperationException>(() =>
            _db.CreateDocumentService().UploadAsync(new UploadDocumentDto
            {
                OriginalFileName = "shell.php",
                ContentType = "text/plain",
                Content = [1]
            }));
    }

    [Test]
    public async Task Upload_DoubleExtensionEndingWithAllowed_Succeeds()
    {
        _db.CurrentUser.UserId = 10;
        var dto = await _db.CreateDocumentService().UploadAsync(new UploadDocumentDto
        {
            OriginalFileName = "report.php.pdf",
            ContentType = "application/pdf",
            Content = [1]
        });
        Assert.That(dto.OriginalFileName, Is.EqualTo("report.php.pdf"));
    }

    [Test]
    public async Task Download_UsesStoredFileNameWithoutOwnershipCheck()
    {
        _db.CurrentUser.UserId = 10;
        var uploaded = await _db.CreateDocumentService().UploadAsync(new UploadDocumentDto
        {
            OriginalFileName = "secret.pdf",
            ContentType = "application/pdf",
            Content = [7, 7, 7]
        });

        var downloaded = await _db.CreateDocumentService().DownloadAsync(uploaded.Id);
        Assert.That(downloaded.Content, Is.EqualTo(new byte[] { 7, 7, 7 }));
        Assert.That(downloaded.FileName, Is.EqualTo("secret.pdf"));
    }

    [Test]
    public void Download_MissingDocument_Throws()
    {
        Assert.ThrowsAsync<KeyNotFoundException>(() => _db.CreateDocumentService().DownloadAsync(1));
    }

    [Test]
    public void Download_TraversalStoredName_IsRejectedByStorageBasename()
    {
        _db.Documents.Seed(new Document
        {
            StoredFileName = "../other/file.pdf",
            OriginalFileName = "file.pdf",
            UploadedByUserId = 1
        });
        _db.FileStorage.Files["../other/file.pdf"] = ([9], "application/pdf", "../other/file.pdf");

        Assert.ThrowsAsync<FileNotFoundException>(() =>
            _db.CreateDocumentService().DownloadAsync(_db.Documents.Items[0].Id));
    }

    [Test]
    public void Upload_Unauthenticated_Throws()
    {
        Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            _db.CreateDocumentService().UploadAsync(new UploadDocumentDto
            {
                OriginalFileName = "a.pdf",
                ContentType = "application/pdf",
                Content = [1]
            }));
    }
}
