using EventHub.Application.Common;

namespace EventHub.Tests.Common;

[TestFixture]
public class PagingQueryTests
{
    [Test]
    public void Default_SkipIsZeroAndTakeIsTen()
    {
        var query = new PagingQuery();
        Assert.That(query.Skip, Is.EqualTo(0));
        Assert.That(query.Take, Is.EqualTo(10));
    }

    [TestCase(0, 10)]
    [TestCase(-3, 10)]
    [TestCase(51, 10)]
    [TestCase(50, 50)]
    [TestCase(1, 1)]
    public void Take_ClampsInvalidPageSize(int pageSize, int expectedTake)
    {
        var query = new PagingQuery { PageSize = pageSize };
        Assert.That(query.Take, Is.EqualTo(expectedTake));
    }

    [Test]
    public void Skip_UsesAtLeastPageOne()
    {
        var query = new PagingQuery { Page = 0, PageSize = 10 };
        Assert.That(query.Skip, Is.EqualTo(0));
    }

    [Test]
    public void Skip_PageThree()
    {
        var query = new PagingQuery { Page = 3, PageSize = 10 };
        Assert.That(query.Skip, Is.EqualTo(20));
    }

    [Test]
    public void AppPermissions_AllContainsKnownCodes()
    {
        Assert.That(AppPermissions.All, Does.Contain(AppPermissions.TicketsPurchase));
        Assert.That(AppPermissions.All, Does.Contain(AppPermissions.WaitlistNotify));
        Assert.That(AppPermissions.All.Distinct().Count(), Is.EqualTo(AppPermissions.All.Length));
    }

    [Test]
    public void FileUploadDefaults_IncludesJpeg()
    {
        Assert.That(FileUploadDefaults.AllowedExtensions, Does.Contain(".jpeg"));
        Assert.That(FileUploadDefaults.AllowedExtensions, Does.Contain(".png"));
        Assert.That(FileUploadDefaults.AllowedExtensions, Does.Contain(".jpg"));
        Assert.That(FileUploadDefaults.AllowedExtensions, Does.Contain(".pdf"));
        Assert.That(FileUploadDefaults.MaxFileSizeInBytes, Is.EqualTo(10 * 1024 * 1024));
    }
}
