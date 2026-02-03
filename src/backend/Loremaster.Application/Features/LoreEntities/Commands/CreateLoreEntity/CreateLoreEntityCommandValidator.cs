using FluentValidation;

namespace Loremaster.Application.Features.LoreEntities.Commands.CreateLoreEntity;

/// <summary>
/// Validator for CreateLoreEntityCommand ensuring required fields are valid.
/// Entity type validation against confirmed templates is performed in the handler
/// since it requires async database access.
/// </summary>
public class CreateLoreEntityCommandValidator : AbstractValidator<CreateLoreEntityCommand>
{
    public CreateLoreEntityCommandValidator()
    {
        RuleFor(x => x.CampaignId)
            .NotEmpty().WithMessage("Campaign ID is required");

        RuleFor(x => x.EntityType)
            .NotEmpty().WithMessage("Entity type is required")
            .MaximumLength(50).WithMessage("Entity type cannot exceed 50 characters")
            .Matches(@"^[a-zA-Z][a-zA-Z0-9_\-\s]*$")
            .WithMessage("Entity type must start with a letter and contain only letters, numbers, underscores, hyphens, or spaces");

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Name is required")
            .MaximumLength(200).WithMessage("Name cannot exceed 200 characters");

        RuleFor(x => x.Description)
            .MaximumLength(5000).WithMessage("Description cannot exceed 5000 characters")
            .When(x => x.Description != null);

        RuleFor(x => x.ImageUrl)
            .MaximumLength(2000).WithMessage("Image URL cannot exceed 2000 characters")
            .Must(BeValidUrl).WithMessage("Image URL must be a valid URL")
            .When(x => !string.IsNullOrEmpty(x.ImageUrl));
    }

    /// <summary>
    /// Validates that the URL is well-formed.
    /// </summary>
    private static bool BeValidUrl(string? url)
    {
        if (string.IsNullOrWhiteSpace(url)) return true;
        return Uri.TryCreate(url, UriKind.Absolute, out var result) 
               && (result.Scheme == Uri.UriSchemeHttp || result.Scheme == Uri.UriSchemeHttps);
    }
}
