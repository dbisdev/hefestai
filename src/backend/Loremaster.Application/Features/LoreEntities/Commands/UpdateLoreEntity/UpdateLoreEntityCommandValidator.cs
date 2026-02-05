using FluentValidation;

namespace Loremaster.Application.Features.LoreEntities.Commands.UpdateLoreEntity;

/// <summary>
/// Validator for UpdateLoreEntityCommand ensuring required fields are valid.
/// </summary>
public class UpdateLoreEntityCommandValidator : AbstractValidator<UpdateLoreEntityCommand>
{
    /// <summary>
    /// Maximum length for regular HTTP/HTTPS URLs.
    /// </summary>
    private const int MaxHttpUrlLength = 2000;
    
    /// <summary>
    /// Maximum length for base64 data URIs (supports images up to ~3.75MB encoded).
    /// Base64 encoding increases size by ~33%, so 5MB limit supports ~3.75MB images.
    /// </summary>
    private const int MaxDataUriLength = 5_000_000;

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
            .Must(BeValidImageSource)
            .WithMessage(x => GetImageValidationErrorMessage(x.ImageUrl))
            .When(x => !string.IsNullOrEmpty(x.ImageUrl));
    }

    /// <summary>
    /// Validates that the image source is either a valid HTTP(S) URL or a valid base64 data URI.
    /// </summary>
    private static bool BeValidImageSource(string? imageUrl)
    {
        if (string.IsNullOrWhiteSpace(imageUrl)) return true;
        
        // Check if it's a data URI (base64 encoded image)
        if (imageUrl.StartsWith("data:", StringComparison.OrdinalIgnoreCase))
        {
            // Validate data URI format: data:[<mediatype>][;base64],<data>
            if (!imageUrl.StartsWith("data:image/", StringComparison.OrdinalIgnoreCase))
            {
                return false; // Must be an image type
            }
            
            // Check length limit for data URIs
            if (imageUrl.Length > MaxDataUriLength)
            {
                return false;
            }
            
            // Verify it contains base64 marker and data
            return imageUrl.Contains(";base64,", StringComparison.OrdinalIgnoreCase);
        }
        
        // For regular URLs, check length and format
        if (imageUrl.Length > MaxHttpUrlLength)
        {
            return false;
        }
        
        return Uri.TryCreate(imageUrl, UriKind.Absolute, out var result) 
               && (result.Scheme == Uri.UriSchemeHttp || result.Scheme == Uri.UriSchemeHttps);
    }

    /// <summary>
    /// Generates a detailed error message for image validation failures.
    /// </summary>
    private static string GetImageValidationErrorMessage(string? imageUrl)
    {
        if (string.IsNullOrWhiteSpace(imageUrl))
            return "Image must be a valid URL or base64 data URI";

        if (imageUrl.StartsWith("data:", StringComparison.OrdinalIgnoreCase))
        {
            if (!imageUrl.StartsWith("data:image/", StringComparison.OrdinalIgnoreCase))
                return $"Data URI must be an image type (starts with 'data:image/'). Got: {imageUrl[..Math.Min(50, imageUrl.Length)]}...";
            
            if (imageUrl.Length > MaxDataUriLength)
                return $"Base64 image exceeds maximum size of {MaxDataUriLength / 1_000_000}MB. Current size: {imageUrl.Length / 1_000_000.0:F2}MB";
            
            if (!imageUrl.Contains(";base64,", StringComparison.OrdinalIgnoreCase))
                return "Data URI must contain ';base64,' marker";
        }
        else
        {
            if (imageUrl.Length > MaxHttpUrlLength)
                return $"URL exceeds maximum length of {MaxHttpUrlLength} characters";
            
            if (!Uri.TryCreate(imageUrl, UriKind.Absolute, out var uri))
                return $"Invalid URL format: {imageUrl[..Math.Min(100, imageUrl.Length)]}";
            
            if (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps)
                return $"URL must use http or https scheme. Got: {uri.Scheme}";
        }
        
        return "Image must be a valid URL or base64 data URI";
    }
}
