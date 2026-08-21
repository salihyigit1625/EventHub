using EventHub.Application.Common;
using EventHub.Application.DTOs.Ticketing;
using FluentValidation;

namespace EventHub.Application.Validations.Ticketing;

public class UploadDocumentDtoValidator : AbstractValidator<UploadDocumentDto>
{
    public UploadDocumentDtoValidator()
    {
        RuleFor(x => x.OriginalFileName).NotEmpty().MaximumLength(255);
        RuleFor(x => x.Content)
            .NotEmpty()
            .Must(content => content.Length <= FileUploadDefaults.MaxFileSizeInBytes)
            .WithMessage("File size must be 10 MB or less.");
    }
}
