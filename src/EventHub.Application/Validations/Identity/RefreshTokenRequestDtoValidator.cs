using EventHub.Application.DTOs.Identity;
using FluentValidation;

namespace EventHub.Application.Validations.Identity;

public class RefreshTokenRequestDtoValidator : AbstractValidator<RefreshTokenRequestDto>
{
    public RefreshTokenRequestDtoValidator()
    {
        RuleFor(x => x.RefreshToken).NotEmpty();
    }
}
