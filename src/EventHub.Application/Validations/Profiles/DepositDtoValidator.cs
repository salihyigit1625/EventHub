using EventHub.Application.DTOs.Ticketing;
using FluentValidation;

namespace EventHub.Application.Validations.Profiles;

public class DepositDtoValidator : AbstractValidator<DepositDto>
{
    public DepositDtoValidator()
    {
        RuleFor(x => x.Amount).GreaterThan(0);
    }
}
