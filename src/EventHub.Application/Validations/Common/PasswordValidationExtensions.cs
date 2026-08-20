using FluentValidation;

namespace EventHub.Application.Validations.Common;

public static class PasswordValidationExtensions
{
    public const int MinLength = 8;
    public const int MaxLength = 128;

    public static IRuleBuilderOptions<T, string> ApplyPasswordPolicy<T>(this IRuleBuilder<T, string> ruleBuilder) =>
        ruleBuilder
            .NotEmpty()
            .MinimumLength(MinLength)
            .MaximumLength(MaxLength)
            .Matches("[A-Z]").WithMessage("Password must contain at least one uppercase letter.")
            .Matches("[a-z]").WithMessage("Password must contain at least one lowercase letter.")
            .Matches(@"[^a-zA-Z0-9]").WithMessage("Password must contain at least one special character.");
}
