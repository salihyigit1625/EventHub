using EventHub.Application.DTOs.Ticketing;
using FluentValidation;

namespace EventHub.Application.Validations.Profiles;

public class CheckInTicketDtoValidator : AbstractValidator<CheckInTicketDto>
{
    public CheckInTicketDtoValidator()
    {
        RuleFor(x => x.UniqueCode).NotEmpty().MaximumLength(64);
        RuleFor(x => x.DeviceLocation).MaximumLength(200);
    }
}
