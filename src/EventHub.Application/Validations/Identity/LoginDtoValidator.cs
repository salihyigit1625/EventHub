using EventHub.Application.DTOs.Identity;
using FluentValidation;

namespace EventHub.Application.Validations.Identity;

public class LoginDtoValidator : AbstractValidator<LoginDto>
{
    public LoginDtoValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.Password).NotEmpty();
    }
}
