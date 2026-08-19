using EventHub.Application.DTOs.Profiles;
using FluentValidation;

namespace EventHub.Application.Validations.Profiles;

public class UpdateOrganizerProfileDtoValidator : AbstractValidator<UpdateOrganizerProfileDto>
{
    public UpdateOrganizerProfileDtoValidator()
    {
        RuleFor(x => x.CompanyName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.TaxNumber).MaximumLength(50).When(x => x.TaxNumber is not null);
    }
}
