using FluentValidation;

namespace Loremaster.Application.Features.Documents.Commands.IngestDocument;

public class IngestDocumentCommandValidator : AbstractValidator<IngestDocumentCommand>
{
    public IngestDocumentCommandValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Title is required")
            .MaximumLength(500).WithMessage("Title must not exceed 500 characters");

        RuleFor(x => x.Content)
            .NotEmpty().WithMessage("Content is required")
            .MaximumLength(100000).WithMessage("Content must not exceed 100,000 characters");

        RuleFor(x => x.OwnerId)
            .NotEmpty().WithMessage("OwnerId is required");

        RuleFor(x => x.Source)
            .MaximumLength(1000).WithMessage("Source must not exceed 1,000 characters")
            .When(x => x.Source != null);
    }
}
