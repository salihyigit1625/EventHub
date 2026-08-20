using EventHub.Application.DTOs.Identity;
using EventHub.Application.Validations.Common;
using FluentValidation;

namespace EventHub.Application.Validations.Identity;

public class RegisterAttendeeDtoValidator : AbstractValidator<RegisterAttendeeDto>
{
    public RegisterAttendeeDtoValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(256);
        RuleFor(x => x.Password).ApplyPasswordPolicy();
        RuleFor(x => x.FullName).NotEmpty().MaximumLength(200);
    }
}
