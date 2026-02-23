namespace Loremaster.Application.Features.EntityGeneration.Services;

public interface IImageGenerationService
{
    Task<ImageGenerationResult> GenerateEntityImageAsync(
        ImageGenerationContext context,
        CancellationToken cancellationToken = default);
}

public record ImageGenerationContext(
    Guid? GameSystemId,
    Guid UserId,
    string EntityType,
    string BasePrompt,
    string? EntityDescription = null,
    string? StyleSearchContext = null,
    IEnumerable<string>? StyleContextChunks = null);

public record ImageGenerationResult(
    string? ImageBase64,
    string? ImageUrl,
    bool Success,
    string? Error = null);
