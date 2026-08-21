using EventHub.Application.Common;
using EventHub.Application.DTOs.Admin;
using EventHub.Application.DTOs.Events;
using EventHub.Application.DTOs.Identity;
using EventHub.Application.DTOs.Profiles;
using EventHub.Application.DTOs.Ticketing;
using EventHub.Application.Validations.Admin;
using EventHub.Application.Validations.Events;
using EventHub.Application.Validations.Identity;
using EventHub.Application.Validations.Profiles;
using EventHub.Application.Validations.Ticketing;

namespace EventHub.Tests.Validations;

[TestFixture]
public class ValidatorTests
{
    [Test]
    public void RegisterAttendee_Valid()
    {
        var result = new RegisterAttendeeDtoValidator().Validate(new RegisterAttendeeDto
        {
            Email = "ada@eventhub.local",
            Password = "Secret1!",
            FullName = "Ada"
        });
        Assert.That(result.IsValid, Is.True);
    }

    [Test]
    public void RegisterAttendee_InvalidEmailAndShortPassword()
    {
        var result = new RegisterAttendeeDtoValidator().Validate(new RegisterAttendeeDto
        {
            Email = "not-an-email",
            Password = "123",
            FullName = ""
        });
        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Errors.Any(e => e.PropertyName == "Email"), Is.True);
        Assert.That(result.Errors.Any(e => e.PropertyName == "Password"), Is.True);
        Assert.That(result.Errors.Any(e => e.PropertyName == "FullName"), Is.True);
    }

    [Test]
    public void RegisterOrganizer_RequiresCompanyName()
    {
        var result = new RegisterOrganizerDtoValidator().Validate(new RegisterOrganizerDto
        {
            Email = "org@eventhub.local",
            Password = "Secret1!",
            FullName = "Org",
            CompanyName = ""
        });
        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Errors.Any(e => e.PropertyName == "CompanyName"), Is.True);
    }

    [Test]
    public void Login_RequiresEmailAndPassword()
    {
        var result = new LoginDtoValidator().Validate(new LoginDto());
        Assert.That(result.IsValid, Is.False);
    }

    [Test]
    public void RefreshToken_Empty_IsInvalid()
    {
        var result = new RefreshTokenRequestDtoValidator().Validate(new RefreshTokenRequestDto());
        Assert.That(result.IsValid, Is.False);
    }

    [Test]
    public void CreateEvent_EndDateMustBeAfterStart()
    {
        var now = DateTime.UtcNow.AddDays(1);
        var result = new CreateEventDtoValidator().Validate(new CreateEventDto
        {
            Title = "Show",
            Venue = "Hall",
            StartDate = now,
            EndDate = now,
            CancellationDeadlineHours = 0
        });
        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Errors.Any(e => e.PropertyName == "EndDate"), Is.True);
    }

    [Test]
    public void CreateEvent_PastStartDate_IsInvalid()
    {
        var result = new CreateEventDtoValidator().Validate(new CreateEventDto
        {
            Title = "Show",
            Venue = "Hall",
            StartDate = DateTime.UtcNow.AddDays(-1),
            EndDate = DateTime.UtcNow.AddDays(1)
        });
        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Errors.Any(e => e.PropertyName == "StartDate"), Is.True);
    }

    [Test]
    public void CreateEvent_Valid()
    {
        var now = DateTime.UtcNow.AddDays(1);
        var result = new CreateEventDtoValidator().Validate(new CreateEventDto
        {
            Title = "Show",
            Venue = "Hall",
            StartDate = now,
            EndDate = now.AddHours(2)
        });
        Assert.That(result.IsValid, Is.True);
    }

    [Test]
    public void UpdateEvent_NegativeDeadline_IsInvalid()
    {
        var now = DateTime.UtcNow.AddDays(1);
        var result = new UpdateEventDtoValidator().Validate(new UpdateEventDto
        {
            Title = "Show",
            Venue = "Hall",
            StartDate = now,
            EndDate = now.AddHours(1),
            CancellationDeadlineHours = -1
        });
        Assert.That(result.IsValid, Is.False);
    }

    [Test]
    public void CreateTicketType_PriceCannotBeNegative()
    {
        var result = new CreateTicketTypeDtoValidator().Validate(new CreateTicketTypeDto
        {
            EventId = 1,
            Name = "GA",
            Price = -1,
            TotalQuantity = 10,
            SaleStartDate = DateTime.UtcNow,
            SaleEndDate = DateTime.UtcNow.AddDays(1)
        });
        Assert.That(result.IsValid, Is.False);
    }

    [Test]
    public void CreateTicketType_ZeroQuantity_IsInvalid()
    {
        var result = new CreateTicketTypeDtoValidator().Validate(new CreateTicketTypeDto
        {
            EventId = 1,
            Name = "GA",
            Price = 0,
            TotalQuantity = 0,
            SaleStartDate = DateTime.UtcNow,
            SaleEndDate = DateTime.UtcNow.AddDays(1)
        });
        Assert.That(result.IsValid, Is.False);
    }

    [Test]
    public void UpdateTicketType_SaleEndMustBeAfterStart()
    {
        var start = DateTime.UtcNow;
        var result = new UpdateTicketTypeDtoValidator().Validate(new UpdateTicketTypeDto
        {
            Name = "GA",
            Price = 1,
            TotalQuantity = 1,
            SaleStartDate = start,
            SaleEndDate = start
        });
        Assert.That(result.IsValid, Is.False);
    }

    [Test]
    public void Deposit_Zero_IsInvalid()
    {
        Assert.That(new DepositDtoValidator().Validate(new DepositDto { Amount = 0 }).IsValid, Is.False);
        Assert.That(new DepositDtoValidator().Validate(new DepositDto { Amount = 0.01m }).IsValid, Is.True);
    }

    [Test]
    public void UploadPoster_EmptyContent_IsInvalid()
    {
        var result = new UploadEventPosterDtoValidator().Validate(new UploadEventPosterDto
        {
            EventId = 1,
            OriginalFileName = "a.png",
            Content = []
        });
        Assert.That(result.IsValid, Is.False);
    }

    [Test]
    public void UploadPoster_TooLarge_IsInvalid()
    {
        var result = new UploadEventPosterDtoValidator().Validate(new UploadEventPosterDto
        {
            EventId = 1,
            OriginalFileName = "a.png",
            Content = new byte[FileUploadDefaults.MaxFileSizeInBytes + 1]
        });
        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Errors.Any(e => e.ErrorMessage.Contains("10 MB")), Is.True);
    }

    [Test]
    public void UploadDocument_ValidSmallPdf()
    {
        var result = new UploadDocumentDtoValidator().Validate(new UploadDocumentDto
        {
            OriginalFileName = "a.pdf",
            Content = [1]
        });
        Assert.That(result.IsValid, Is.True);
    }

    [Test]
    public void CreateGateStaff_InvalidEmail()
    {
        var result = new CreateGateStaffDtoValidator().Validate(new CreateGateStaffDto
        {
            Email = "x",
            Password = "Secret1!",
            FullName = "Gate"
        });
        Assert.That(result.IsValid, Is.False);
    }

    [Test]
    public void AssignGateStaff_RequiresPositiveIds()
    {
        var result = new AssignGateStaffDtoValidator().Validate(new AssignGateStaffDto());
        Assert.That(result.IsValid, Is.False);
    }

    [Test]
    public void CheckIn_RequiresCode()
    {
        var result = new CheckInTicketDtoValidator().Validate(new CheckInTicketDto());
        Assert.That(result.IsValid, Is.False);
    }

    [Test]
    public void UpdateOrganizer_RequiresCompanyName()
    {
        var result = new UpdateOrganizerProfileDtoValidator().Validate(new UpdateOrganizerProfileDto
        {
            CompanyName = ""
        });
        Assert.That(result.IsValid, Is.False);
    }
}
