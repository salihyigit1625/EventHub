using EventHub.Application.DTOs.Identity;
using FluentValidation;

namespace EventHub.Application.Validations.Identity;

public class RegisterOrganizerDtoValidator : AbstractValidator<RegisterOrganizerDto>
{
    public RegisterOrganizerDtoValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(256);
        RuleFor(x => x.Password).NotEmpty().MinimumLength(6).MaximumLength(100);
        RuleFor(x => x.FullName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.CompanyName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.TaxNumber).MaximumLength(50);
    }
}
