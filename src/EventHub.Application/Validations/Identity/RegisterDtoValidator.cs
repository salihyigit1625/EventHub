using EventHub.Application.Common;
using EventHub.Application.DTOs.Identity;
using FluentValidation;

namespace EventHub.Application.Validations.Identity;

public class RegisterDtoValidator : AbstractValidator<RegisterDto>
{
    public RegisterDtoValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(256);
        RuleFor(x => x.Password).NotEmpty().MinimumLength(6).MaximumLength(100);
        RuleFor(x => x.FullName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Role)
            .NotEmpty()
            .Must(role => AppRoles.Registrable.Contains(role))
            .WithMessage("Role must be Attendee or Organizer.");
    }
}
