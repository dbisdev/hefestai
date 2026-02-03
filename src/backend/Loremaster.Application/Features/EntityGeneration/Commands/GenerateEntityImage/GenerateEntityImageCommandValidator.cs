using FluentValidation;

namespace Loremaster.Application.Features.EntityGeneration.Commands.GenerateEntityImage;

/// <summary>
/// Validator for GenerateEntityImageCommand.
/// </summary>
public class GenerateEntityImageCommandValidator : AbstractValidator<GenerateEntityImageCommand>
{
    /// <summary>
    /// Valid style options for image generation.
    /// </summary>
    private static readonly string[] ValidStyles = { "fantasy", "realistic", "anime", "sketch" };

    public GenerateEntityImageCommandValidator()
    {
        RuleFor(x => x.CampaignId)
            .NotEmpty()
            .WithMessage("Campaign ID is required");

        RuleFor(x => x.EntityId)
            .NotEmpty()
            .WithMessage("Entity ID is required");

        RuleFor(x => x.Style)
            .Must(style => style == null || ValidStyles.Contains(style.ToLowerInvariant()))
            .WithMessage($"Style must be one of: {string.Join(", ", ValidStyles)}")
            .When(x => x.Style != null);
    }
}
