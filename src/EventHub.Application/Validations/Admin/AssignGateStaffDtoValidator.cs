using EventHub.Application.DTOs.Admin;
using FluentValidation;

namespace EventHub.Application.Validations.Admin;

public class AssignGateStaffDtoValidator : AbstractValidator<AssignGateStaffDto>
{
    public AssignGateStaffDtoValidator()
    {
        RuleFor(x => x.GateStaffUserId).GreaterThan(0);
        RuleFor(x => x.EventId).GreaterThan(0);
    }
}
