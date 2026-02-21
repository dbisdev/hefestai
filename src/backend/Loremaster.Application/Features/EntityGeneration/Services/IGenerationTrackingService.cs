using Loremaster.Domain.Entities;

namespace Loremaster.Application.Features.EntityGeneration.Services;

public interface IGenerationTrackingService
{
    GenerationRequest CreateRequest(
        Guid userId,
        string entityType,
        string fullPrompt);

    GenerationResult CreateResult(
        Guid generationRequestId,
        string resultJson,
        bool ragContextUsed,
        int ragSourceCount,
        bool hasImage,
        string modelName = DefaultGenerationConstants.ModelName,
        float temperature = DefaultGenerationConstants.Temperature,
        int maxTokens = DefaultGenerationConstants.MaxTokens);

    Task PersistAsync(
        GenerationRequest request,
        GenerationResult result,
        CancellationToken cancellationToken = default);
}

public static class DefaultGenerationConstants
{
    public const string ModelName = "gemini-2.0-flash";
    public const float Temperature = 0.8f;
    public const int MaxTokens = 2048;
}
