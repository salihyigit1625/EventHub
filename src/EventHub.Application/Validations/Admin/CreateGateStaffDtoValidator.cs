using EventHub.Application.DTOs.Admin;
using EventHub.Application.Validations.Common;
using FluentValidation;

namespace EventHub.Application.Validations.Admin;

public class CreateGateStaffDtoValidator : AbstractValidator<CreateGateStaffDto>
{
    public CreateGateStaffDtoValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(256);
        RuleFor(x => x.Password).ApplyPasswordPolicy();
        RuleFor(x => x.FullName).NotEmpty().MaximumLength(200);
    }
}
