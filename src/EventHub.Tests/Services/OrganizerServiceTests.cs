using EventHub.Application.DTOs.Profiles;
using EventHub.Tests.Fakes;

namespace EventHub.Tests.Services;

[TestFixture]
public class OrganizerServiceTests
{
    private TestDb _db = null!;

    [SetUp]
    public void SetUp() => _db = new TestDb();

    [Test]
    public async Task GetProfile_ReturnsOrganizerByUserId()
    {
        _db.SeedApprovedOrganizer(10, "Acme");

        var dto = await _db.CreateOrganizerService().GetProfileAsync(10);

        Assert.That(dto.CompanyName, Is.EqualTo("Acme"));
        Assert.That(dto.Email, Is.EqualTo("org10@eventhub.local"));
    }

    [Test]
    public void GetProfile_Unknown_Throws()
    {
        Assert.ThrowsAsync<KeyNotFoundException>(() => _db.CreateOrganizerService().GetProfileAsync(10));
    }

    [Test]
    public async Task UpdateProfile_UsesCurrentUser()
    {
        _db.SeedApprovedOrganizer(10);
        _db.CurrentUser.UserId = 10;

        var dto = await _db.CreateOrganizerService().UpdateProfileAsync(new UpdateOrganizerProfileDto
        {
            CompanyName = "  New Co  ",
            TaxNumber = "  VN-1  "
        });

        Assert.That(dto.CompanyName, Is.EqualTo("New Co"));
        Assert.That(dto.TaxNumber, Is.EqualTo("VN-1"));
    }

    [Test]
    public async Task UpdateProfile_BlankTaxNumber_BecomesNull()
    {
        _db.SeedApprovedOrganizer(10);
        _db.CurrentUser.UserId = 10;

        var dto = await _db.CreateOrganizerService().UpdateProfileAsync(new UpdateOrganizerProfileDto
        {
            CompanyName = "Co",
            TaxNumber = "  "
        });

        Assert.That(dto.TaxNumber, Is.Null);
    }
}
