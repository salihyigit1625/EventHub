using EventHub.Application.DTOs.Ticketing;
using EventHub.Domain.Entities.Ticketing;
using EventHub.Tests.Fakes;

namespace EventHub.Tests.Services;

[TestFixture]
public class DocumentServiceTests
{
    private TestDb _db = null!;

    private static readonly byte[] ValidPdf = [(byte)'%', (byte)'P', (byte)'D', (byte)'F', (byte)'-', (byte)'1'];
    private static readonly byte[] ValidPng =
    [
        0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0x00, 0x00, 0x00, 0x0D
    ];

    [SetUp]
    public void SetUp() => _db = new TestDb();

    [Test]
    public async Task Upload_StoresGuidNameAndSafeMetadata()
    {
        _db.CurrentUser.UserId = 10;

        var dto = await _db.CreateDocumentService().UploadAsync(new UploadDocumentDto
        {
            OriginalFileName = "sub/../../notes.pdf",
            Content = ValidPdf
        });

        Assert.That(dto.OriginalFileName, Is.EqualTo("notes.pdf"));
        Assert.That(dto.ContentType, Is.EqualTo("application/pdf"));
        Assert.That(dto.FileSizeInBytes, Is.EqualTo(ValidPdf.Length));
        Assert.That(dto.UploadedByUserId, Is.EqualTo(10));
        Assert.That(_db.Documents.Items.Single().StoredFileName, Does.Match("^[a-f0-9]{32}\\.pdf$"));
    }

    [Test]
    public void Upload_DisallowedExtension_Throws()
    {
        _db.CurrentUser.UserId = 10;
        Assert.ThrowsAsync<InvalidOperationException>(() =>
            _db.CreateDocumentService().UploadAsync(new UploadDocumentDto
            {
                OriginalFileName = "shell.php",
                Content = ValidPdf
            }));
    }

    [Test]
    public void Upload_InvalidMagicBytes_Throws()
    {
        _db.CurrentUser.UserId = 10;
        Assert.ThrowsAsync<InvalidOperationException>(() =>
            _db.CreateDocumentService().UploadAsync(new UploadDocumentDto
            {
                OriginalFileName = "notes.pdf",
                Content = [0x4D, 0x5A, 0x00, 0x00]
            }));
    }

    [Test]
    public async Task Upload_ValidPng_Succeeds()
    {
        _db.CurrentUser.UserId = 10;
        var dto = await _db.CreateDocumentService().UploadAsync(new UploadDocumentDto
        {
            OriginalFileName = "poster.png",
            Content = ValidPng
        });
        Assert.That(dto.ContentType, Is.EqualTo("image/png"));
    }

    [Test]
    public async Task GetMyDocuments_ReturnsOnlyCurrentUserDocs()
    {
        _db.CurrentUser.UserId = 10;
        await _db.CreateDocumentService().UploadAsync(new UploadDocumentDto
        {
            OriginalFileName = "mine.pdf",
            Content = ValidPdf
        });

        _db.CurrentUser.UserId = 20;
        await _db.CreateDocumentService().UploadAsync(new UploadDocumentDto
        {
            OriginalFileName = "theirs.pdf",
            Content = ValidPdf
        });

        _db.CurrentUser.UserId = 10;
        var list = await _db.CreateDocumentService().GetMyDocumentsAsync();

        Assert.That(list, Has.Count.EqualTo(1));
        Assert.That(list.Single().OriginalFileName, Is.EqualTo("mine.pdf"));
        Assert.That(list.Single().UploadedByUserId, Is.EqualTo(10));
    }

    [Test]
    public async Task Download_OwnDocument_Succeeds()
    {
        _db.CurrentUser.UserId = 10;
        var uploaded = await _db.CreateDocumentService().UploadAsync(new UploadDocumentDto
        {
            OriginalFileName = "secret.pdf",
            Content = ValidPdf
        });

        var downloaded = await _db.CreateDocumentService().DownloadAsync(uploaded.Id);
        Assert.That(downloaded.Content, Is.EqualTo(ValidPdf));
        Assert.That(downloaded.ContentType, Is.EqualTo("application/pdf"));
    }

    [Test]
    public async Task Download_OtherUsersDocument_Throws()
    {
        _db.CurrentUser.UserId = 10;
        var uploaded = await _db.CreateDocumentService().UploadAsync(new UploadDocumentDto
        {
            OriginalFileName = "secret.pdf",
            Content = ValidPdf
        });

        _db.CurrentUser.UserId = 99;
        Assert.ThrowsAsync<KeyNotFoundException>(() =>
            _db.CreateDocumentService().DownloadAsync(uploaded.Id));
    }

    [Test]
    public void Download_MissingDocument_Throws()
    {
        _db.CurrentUser.UserId = 10;
        Assert.ThrowsAsync<KeyNotFoundException>(() => _db.CreateDocumentService().DownloadAsync(1));
    }

    [Test]
    public void Upload_Unauthenticated_Throws()
    {
        Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            _db.CreateDocumentService().UploadAsync(new UploadDocumentDto
            {
                OriginalFileName = "a.pdf",
                Content = ValidPdf
            }));
    }
}
