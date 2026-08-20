using EventHub.Application.DTOs.Identity;
using EventHub.Application.Validations.Common;
using FluentValidation;

namespace EventHub.Application.Validations.Identity;

public class RegisterOrganizerDtoValidator : AbstractValidator<RegisterOrganizerDto>
{
    public RegisterOrganizerDtoValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(256);
        RuleFor(x => x.Password).ApplyPasswordPolicy();
        RuleFor(x => x.FullName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.CompanyName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.TaxNumber).MaximumLength(50);
    }
}
