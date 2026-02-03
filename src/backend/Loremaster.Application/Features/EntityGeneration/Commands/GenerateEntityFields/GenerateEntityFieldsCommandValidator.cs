using FluentValidation;

namespace Loremaster.Application.Features.EntityGeneration.Commands.GenerateEntityFields;

/// <summary>
/// Validator for GenerateEntityFieldsCommand.
/// </summary>
public class GenerateEntityFieldsCommandValidator : AbstractValidator<GenerateEntityFieldsCommand>
{
    public GenerateEntityFieldsCommandValidator()
    {
        RuleFor(x => x.CampaignId)
            .NotEmpty()
            .WithMessage("Campaign ID is required");

        RuleFor(x => x.EntityTypeName)
            .NotEmpty()
            .WithMessage("Entity type name is required")
            .MaximumLength(100)
            .WithMessage("Entity type name must not exceed 100 characters");

        RuleFor(x => x.UserPrompt)
            .MaximumLength(2000)
            .WithMessage("User prompt must not exceed 2000 characters")
            .When(x => x.UserPrompt != null);

        RuleFor(x => x.Temperature)
            .InclusiveBetween(0.0f, 1.0f)
            .WithMessage("Temperature must be between 0.0 and 1.0");

        RuleFor(x => x.ImageStyle)
            .MaximumLength(50)
            .WithMessage("Image style must not exceed 50 characters")
            .When(x => x.ImageStyle != null);
    }
}
