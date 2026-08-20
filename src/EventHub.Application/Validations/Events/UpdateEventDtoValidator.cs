using EventHub.Application.DTOs.Events;
using FluentValidation;

namespace EventHub.Application.Validations.Events;

public class UpdateEventDtoValidator : AbstractValidator<UpdateEventDto>
{
    private static readonly TimeSpan MaxDuration = TimeSpan.FromDays(30);

    public UpdateEventDtoValidator()
    {
        RuleFor(x => x.Title).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Description).MaximumLength(4000);
        RuleFor(x => x.Venue).NotEmpty().MaximumLength(300);
        RuleFor(x => x.StartDate)
            .Must(start => start >= DateTime.UtcNow)
            .WithMessage("Start date must be in the future.");
        RuleFor(x => x.EndDate).GreaterThan(x => x.StartDate);
        RuleFor(x => x)
            .Must(x => x.EndDate - x.StartDate <= MaxDuration)
            .WithMessage("Event duration cannot exceed 30 days.")
            .OverridePropertyName(nameof(UpdateEventDto.EndDate));
        RuleFor(x => x.CancellationDeadlineHours).GreaterThanOrEqualTo(0);
    }
}
