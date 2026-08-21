using EventHub.Application.DTOs.Events;
using FluentValidation;

namespace EventHub.Application.Validations.Events;

public class UpdateTicketTypeDtoValidator : AbstractValidator<UpdateTicketTypeDto>
{
    public UpdateTicketTypeDtoValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(150);
        RuleFor(x => x.Price).GreaterThanOrEqualTo(0);
        RuleFor(x => x.TotalQuantity).GreaterThan(0);
        RuleFor(x => x.MaxTicketsPerUser).GreaterThan(0);
        RuleFor(x => x.SaleEndDate).GreaterThan(x => x.SaleStartDate);
    }
}
