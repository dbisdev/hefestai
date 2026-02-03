using FluentValidation;

namespace Loremaster.Application.Features.LoreEntities.Commands.UpdateLoreEntity;

/// <summary>
/// Validator for UpdateLoreEntityCommand ensuring required fields are valid.
/// </summary>
public class UpdateLoreEntityCommandValidator : AbstractValidator<UpdateLoreEntityCommand>
{
    public UpdateLoreEntityCommandValidator()
    {
        RuleFor(x => x.CampaignId)
            .NotEmpty().WithMessage("Campaign ID is required");

        RuleFor(x => x.EntityId)
            .NotEmpty().WithMessage("Entity ID is required");

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
