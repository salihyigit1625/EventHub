using EventHub.Application.Common;
using EventHub.Application.DTOs.Ticketing;
using FluentValidation;

namespace EventHub.Application.Validations.Profiles;

public class DepositDtoValidator : AbstractValidator<DepositDto>
{
    public DepositDtoValidator()
    {
        RuleFor(x => x.Amount)
            .GreaterThan(0)
            .LessThanOrEqualTo(WalletDefaults.MaxDepositAmount)
            .WithMessage($"Deposit amount must be between 0.01 and {WalletDefaults.MaxDepositAmount}.");
    }
}
