using Loremaster.Application.Common.Interfaces;
using Microsoft.Extensions.Logging;

namespace Loremaster.Application.Features.EntityGeneration.Services;

public class ImageGenerationService : IImageGenerationService
{
    private readonly IAiService _aiService;
    private readonly IRagContextProvider _ragContextProvider;
    private readonly ILogger<ImageGenerationService> _logger;

    public ImageGenerationService(
        IAiService aiService,
        IRagContextProvider ragContextProvider,
        ILogger<ImageGenerationService> logger)
    {
        _aiService = aiService;
        _ragContextProvider = ragContextProvider;
        _logger = logger;
    }

    public async Task<ImageGenerationResult> GenerateEntityImageAsync(
        ImageGenerationContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var imagePrompt = await BuildImagePromptAsync(context, cancellationToken);
            var imageResult = await _aiService.GenerateImageAsync(imagePrompt, cancellationToken: cancellationToken);

            return new ImageGenerationResult(
                imageResult.ImageBase64,
                imageResult.ImageUrl,
                imageResult.Success);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate image for entity type {EntityType}", context.EntityType);
            return new ImageGenerationResult(null, null, false, ex.Message);
        }
    }

    private async Task<string> BuildImagePromptAsync(
        ImageGenerationContext context,
        CancellationToken cancellationToken)
    {
        var styleContext = await GetStyleContextAsync(context, cancellationToken);
        var descriptionContext = ExtractDescriptionContext(context.EntityDescription);

        return $"{context.BasePrompt}{descriptionContext}{styleContext}";
    }

    private async Task<string> GetStyleContextAsync(
        ImageGenerationContext context,
        CancellationToken cancellationToken)
    {
        if (!context.GameSystemId.HasValue)
            return string.Empty;

        try
        {
            var styleContext = await _ragContextProvider.GetStyleContextAsync(
                context.GameSystemId.Value,
                context.UserId,
                context.EntityType,
                context.StyleSearchContext,
                cancellationToken);

            if (!styleContext.Any())
                return string.Empty;

            var contextText = string.Join(" ",
                styleContext.Take(5).Select(c =>
                    c.Content?.Substring(0, Math.Min(400, c.Content.Length)) ?? string.Empty));

            return $" Art style based on: {contextText}";
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Failed to get style context for image generation");
            return string.Empty;
        }
    }

    private static string ExtractDescriptionContext(string? description)
    {
        if (string.IsNullOrEmpty(description))
            return string.Empty;

        var truncated = description.Length > 500
            ? description[..497] + "..."
            : description;

        return $" Character details: {truncated}";
    }
}
