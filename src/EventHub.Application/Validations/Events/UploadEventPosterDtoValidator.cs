using EventHub.Application.Common;
using EventHub.Application.DTOs.Events;
using FluentValidation;

namespace EventHub.Application.Validations.Events;

public class UploadEventPosterDtoValidator : AbstractValidator<UploadEventPosterDto>
{
    public UploadEventPosterDtoValidator()
    {
        RuleFor(x => x.EventId).GreaterThan(0);
        RuleFor(x => x.OriginalFileName).NotEmpty().MaximumLength(255);
        RuleFor(x => x.ContentType).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Content)
            .NotEmpty()
            .Must(content => content.Length <= FileUploadDefaults.MaxFileSizeInBytes)
            .WithMessage("File size must be 10 MB or less.");
    }
}
