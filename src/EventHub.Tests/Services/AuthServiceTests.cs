using EventHub.Application.Common;
using EventHub.Application.DTOs.Identity;
using EventHub.Domain.Entities.Identity;
using EventHub.Tests.Fakes;

namespace EventHub.Tests.Services;

[TestFixture]
public class AuthServiceTests
{
    private TestDb _db = null!;

    [SetUp]
    public void SetUp() => _db = new TestDb();

    [Test]
    public async Task RegisterAttendee_CreatesUserProfileAndReturnsTokens()
    {
        var result = await _db.CreateAuthService().RegisterAttendeeAsync(new RegisterAttendeeDto
        {
            Email = "  Ada@EventHub.Local  ",
            Password = "Secret1!",
            FullName = " Ada Lovelace "
        });

        Assert.That(result.Email, Is.EqualTo("ada@eventhub.local"));
        Assert.That(result.FullName, Is.EqualTo("Ada Lovelace"));
        Assert.That(result.Roles, Is.EquivalentTo(new[] { AppRoles.Attendee }));
        Assert.That(result.AccessToken, Does.StartWith("access-"));
        Assert.That(result.RefreshToken, Is.EqualTo("refresh-1"));
        Assert.That(_db.Attendees.Items, Has.Count.EqualTo(1));
        Assert.That(_db.Attendees.Items[0].WalletBalance, Is.EqualTo(0m));
        Assert.That(_db.Users.Items[0].RefreshToken, Is.EqualTo(result.RefreshToken));
        Assert.That(_db.Users.Items[0].RefreshTokenExpiryTime, Is.Not.Null);
        Assert.That(_db.UserRoles.Items.Any(ur => ur.RoleId == 3), Is.True);
    }

    [Test]
    public async Task RegisterOrganizer_CreatesUnapprovedProfile()
    {
        var result = await _db.CreateAuthService().RegisterOrganizerAsync(new RegisterOrganizerDto
        {
            Email = "org@eventhub.local",
            Password = "Secret1!",
            FullName = "Org User",
            CompanyName = "  Acme  ",
            TaxNumber = "  123  "
        });

        Assert.That(result.Roles, Is.EquivalentTo(new[] { AppRoles.Organizer }));
        var profile = _db.Organizers.Items.Single();
        Assert.That(profile.CompanyName, Is.EqualTo("Acme"));
        Assert.That(profile.TaxNumber, Is.EqualTo("123"));
        Assert.That(profile.IsApproved, Is.False);
    }

    [Test]
    public async Task RegisterOrganizer_WhitespaceTaxNumber_IsStoredAsNull()
    {
        await _db.CreateAuthService().RegisterOrganizerAsync(new RegisterOrganizerDto
        {
            Email = "org2@eventhub.local",
            Password = "Secret1!",
            FullName = "Org",
            CompanyName = "Co",
            TaxNumber = "   "
        });

        Assert.That(_db.Organizers.Items.Single().TaxNumber, Is.Null);
    }

    [Test]
    public void RegisterAttendee_DuplicateEmail_ThrowsGenericMessage()
    {
        _db.Users.Seed(new User { Email = "ada@eventhub.local", FullName = "Existing" });

        var ex = Assert.ThrowsAsync<InvalidOperationException>(() =>
            _db.CreateAuthService().RegisterAttendeeAsync(new RegisterAttendeeDto
            {
                Email = "ADA@eventhub.local",
                Password = "Secret1!",
                FullName = "Ada"
            }));

        Assert.That(ex!.Message, Is.EqualTo(AuthMessages.RegistrationFailed));
    }

    [Test]
    public void RegisterAttendee_MissingRole_Throws()
    {
        _db.Roles.Remove(_db.Roles.Items.Single(r => r.Name == AppRoles.Attendee));

        Assert.ThrowsAsync<KeyNotFoundException>(() =>
            _db.CreateAuthService().RegisterAttendeeAsync(new RegisterAttendeeDto
            {
                Email = "new@eventhub.local",
                Password = "Secret1!",
                FullName = "New"
            }));
    }

    [Test]
    public async Task Login_ValidCredentials_RotatesRefreshToken()
    {
        await _db.CreateAuthService().RegisterAttendeeAsync(new RegisterAttendeeDto
        {
            Email = "ada@eventhub.local",
            Password = "Secret1!",
            FullName = "Ada"
        });
        var firstRefresh = _db.Users.Items[0].RefreshToken;

        var login = await _db.CreateAuthService().LoginAsync(new LoginDto
        {
            Email = "ADA@eventhub.local",
            Password = "Secret1!"
        });

        Assert.That(login.Roles, Is.EquivalentTo(new[] { AppRoles.Attendee }));
        Assert.That(login.RefreshToken, Is.Not.EqualTo(firstRefresh));
        Assert.That(_db.Users.Items[0].RefreshToken, Is.EqualTo(login.RefreshToken));
    }

    [Test]
    public async Task Login_WrongPassword_Throws()
    {
        await _db.CreateAuthService().RegisterAttendeeAsync(new RegisterAttendeeDto
        {
            Email = "ada@eventhub.local",
            Password = "Secret1!",
            FullName = "Ada"
        });

        Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            _db.CreateAuthService().LoginAsync(new LoginDto
            {
                Email = "ada@eventhub.local",
                Password = "wrong"
            }));
    }

    [Test]
    public void Login_UnknownEmail_Throws()
    {
        Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            _db.CreateAuthService().LoginAsync(new LoginDto
            {
                Email = "missing@eventhub.local",
                Password = "Secret1!"
            }));
    }

    [Test]
    public async Task Login_InactiveUser_Throws()
    {
        await _db.CreateAuthService().RegisterAttendeeAsync(new RegisterAttendeeDto
        {
            Email = "ada@eventhub.local",
            Password = "Secret1!",
            FullName = "Ada"
        });
        _db.Users.Items[0].IsActive = false;

        Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            _db.CreateAuthService().LoginAsync(new LoginDto
            {
                Email = "ada@eventhub.local",
                Password = "Secret1!"
            }));
    }

    [Test]
    public async Task RefreshToken_Valid_IssuesNewTokens()
    {
        var registered = await _db.CreateAuthService().RegisterAttendeeAsync(new RegisterAttendeeDto
        {
            Email = "ada@eventhub.local",
            Password = "Secret1!",
            FullName = "Ada"
        });

        var refreshed = await _db.CreateAuthService().RefreshTokenAsync(new RefreshTokenRequestDto
        {
            RefreshToken = registered.RefreshToken
        });

        Assert.That(refreshed.RefreshToken, Is.Not.EqualTo(registered.RefreshToken));
        Assert.That(_db.Users.Items[0].RefreshToken, Is.EqualTo(refreshed.RefreshToken));
    }

    [Test]
    public void RefreshToken_Unknown_Throws()
    {
        Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            _db.CreateAuthService().RefreshTokenAsync(new RefreshTokenRequestDto { RefreshToken = "nope" }));
    }

    [Test]
    public async Task RefreshToken_Expired_Throws()
    {
        var registered = await _db.CreateAuthService().RegisterAttendeeAsync(new RegisterAttendeeDto
        {
            Email = "ada@eventhub.local",
            Password = "Secret1!",
            FullName = "Ada"
        });
        _db.Users.Items[0].RefreshTokenExpiryTime = DateTime.UtcNow.AddMinutes(-1);

        Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            _db.CreateAuthService().RefreshTokenAsync(new RefreshTokenRequestDto
            {
                RefreshToken = registered.RefreshToken
            }));
    }

    [Test]
    public async Task RefreshToken_InactiveUser_Throws()
    {
        var registered = await _db.CreateAuthService().RegisterAttendeeAsync(new RegisterAttendeeDto
        {
            Email = "ada@eventhub.local",
            Password = "Secret1!",
            FullName = "Ada"
        });
        _db.Users.Items[0].IsActive = false;

        Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            _db.CreateAuthService().RefreshTokenAsync(new RefreshTokenRequestDto
            {
                RefreshToken = registered.RefreshToken
            }));
    }

    [Test]
    public async Task Logout_ClearsRefreshToken()
    {
        var registered = await _db.CreateAuthService().RegisterAttendeeAsync(new RegisterAttendeeDto
        {
            Email = "ada@eventhub.local",
            Password = "Secret1!",
            FullName = "Ada"
        });
        _db.CurrentUser.UserId = registered.UserId;
        var tokenCallsBefore = _db.TokenService.AccessTokenCalls;

        await _db.CreateAuthService().LogoutAsync();

        Assert.That(_db.Users.Items[0].RefreshToken, Is.Null);
        Assert.That(_db.Users.Items[0].RefreshTokenExpiryTime, Is.Null);
        Assert.That(_db.TokenService.AccessTokenCalls, Is.EqualTo(tokenCallsBefore));
    }

    [Test]
    public void Logout_Unauthenticated_Throws()
    {
        Assert.ThrowsAsync<UnauthorizedAccessException>(() => _db.CreateAuthService().LogoutAsync());
    }

    [Test]
    public async Task GetCurrentUser_ReturnsRoles()
    {
        var registered = await _db.CreateAuthService().RegisterAttendeeAsync(new RegisterAttendeeDto
        {
            Email = "ada@eventhub.local",
            Password = "Secret1!",
            FullName = "Ada"
        });
        _db.CurrentUser.UserId = registered.UserId;

        var me = await _db.CreateAuthService().GetCurrentUserAsync();

        Assert.That(me.Email, Is.EqualTo("ada@eventhub.local"));
        Assert.That(me.Roles, Is.EquivalentTo(new[] { AppRoles.Attendee }));
        Assert.That(me.IsActive, Is.True);
    }

    [Test]
    public void GetCurrentUser_MissingUser_Throws()
    {
        _db.CurrentUser.UserId = 99;
        Assert.ThrowsAsync<KeyNotFoundException>(() => _db.CreateAuthService().GetCurrentUserAsync());
    }

    [Test]
    public void GetCurrentUser_Unauthenticated_Throws()
    {
        Assert.ThrowsAsync<UnauthorizedAccessException>(() => _db.CreateAuthService().GetCurrentUserAsync());
    }

    [Test]
    public async Task Login_UnknownEmailAndWrongPassword_UseSameMessage()
    {
        await _db.CreateAuthService().RegisterAttendeeAsync(new RegisterAttendeeDto
        {
            Email = "ada@eventhub.local",
            Password = "Secret1!",
            FullName = "Ada"
        });

        var unknown = Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            _db.CreateAuthService().LoginAsync(new LoginDto
            {
                Email = "missing@eventhub.local",
                Password = "Secret1!"
            }));
        var wrong = Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            _db.CreateAuthService().LoginAsync(new LoginDto
            {
                Email = "ada@eventhub.local",
                Password = "wrong"
            }));

        Assert.That(unknown!.Message, Is.EqualTo(wrong!.Message));
        Assert.That(unknown.Message, Is.EqualTo("Invalid email or password."));
    }

    [Test]
    public async Task Login_InactiveUser_UsesSameGenericMessage()
    {
        await _db.CreateAuthService().RegisterAttendeeAsync(new RegisterAttendeeDto
        {
            Email = "ada@eventhub.local",
            Password = "Secret1!",
            FullName = "Ada"
        });
        _db.Users.Items[0].IsActive = false;

        var inactive = Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            _db.CreateAuthService().LoginAsync(new LoginDto
            {
                Email = "ada@eventhub.local",
                Password = "Secret1!"
            }));
        var unknown = Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            _db.CreateAuthService().LoginAsync(new LoginDto
            {
                Email = "nope@eventhub.local",
                Password = "Secret1!"
            }));

        Assert.That(inactive!.Message, Is.EqualTo(unknown!.Message));
    }

    [Test]
    public void RegisterOrganizer_DuplicateEmail_ThrowsGenericMessage()
    {
        _db.Users.Seed(new User { Email = "org@eventhub.local", FullName = "Existing" });

        var ex = Assert.ThrowsAsync<InvalidOperationException>(() =>
            _db.CreateAuthService().RegisterOrganizerAsync(new RegisterOrganizerDto
            {
                Email = "ORG@eventhub.local",
                Password = "Secret1!",
                FullName = "Org",
                CompanyName = "Co"
            }));

        Assert.That(ex!.Message, Is.EqualTo(AuthMessages.RegistrationFailed));
    }

    [Test]
    public async Task RefreshToken_ReusedAfterRotation_Throws()
    {
        var registered = await _db.CreateAuthService().RegisterAttendeeAsync(new RegisterAttendeeDto
        {
            Email = "ada@eventhub.local",
            Password = "Secret1!",
            FullName = "Ada"
        });
        var oldRefresh = registered.RefreshToken;

        await _db.CreateAuthService().RefreshTokenAsync(new RefreshTokenRequestDto
        {
            RefreshToken = oldRefresh
        });

        Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            _db.CreateAuthService().RefreshTokenAsync(new RefreshTokenRequestDto
            {
                RefreshToken = oldRefresh
            }));
    }

    [Test]
    public async Task RegisterAttendee_StoresHashedPassword_NotPlaintext()
    {
        await _db.CreateAuthService().RegisterAttendeeAsync(new RegisterAttendeeDto
        {
            Email = "ada@eventhub.local",
            Password = "Secret1!",
            FullName = "Ada"
        });

        Assert.That(_db.Users.Items[0].PasswordHash, Is.Not.EqualTo("Secret1!"));
        Assert.That(_db.Users.Items[0].PasswordHash, Is.Not.Empty);
    }
}
