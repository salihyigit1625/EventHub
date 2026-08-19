using EventHub.Application.DTOs.Events;
using FluentValidation;

namespace EventHub.Application.Validations.Events;

public class CreateTicketTypeDtoValidator : AbstractValidator<CreateTicketTypeDto>
{
    public CreateTicketTypeDtoValidator()
    {
        RuleFor(x => x.EventId).GreaterThan(0);
        RuleFor(x => x.Name).NotEmpty().MaximumLength(150);
        RuleFor(x => x.Price).GreaterThanOrEqualTo(0);
        RuleFor(x => x.TotalQuantity).GreaterThan(0);
        RuleFor(x => x.SaleEndDate).GreaterThan(x => x.SaleStartDate);
    }
}
