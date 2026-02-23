using Loremaster.Application.Common.Interfaces;
using Loremaster.Application.Features.EntityGeneration.Commands.Shared;
using Loremaster.Application.Features.EntityGeneration.Services;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Loremaster.Application.Features.EntityGeneration.Commands.GenerateEnemy;

public class GenerateEnemyCommandHandler : IRequestHandler<GenerateEnemyCommand, EnemyGenerationResult>
{
    private readonly IAiService _aiService;
    private readonly IRagContextProvider _ragContextProvider;
    private readonly IEmbeddingService _embeddingService;
    private readonly ITemplateResolutionService _templateResolution;
    private readonly IPromptBuilderService _promptBuilder;
    private readonly IImageGenerationService _imageGeneration;
    private readonly IGenerationTrackingService _tracking;
    private readonly ILogger<GenerateEnemyCommandHandler> _logger;

    public GenerateEnemyCommandHandler(
        IAiService aiService,
        IRagContextProvider ragContextProvider,
        IEmbeddingService embeddingService,
        ITemplateResolutionService templateResolution,
        IPromptBuilderService promptBuilder,
        IImageGenerationService imageGeneration,
        IGenerationTrackingService tracking,
        ILogger<GenerateEnemyCommandHandler> logger)
    {
        _aiService = aiService;
        _ragContextProvider = ragContextProvider;
        _embeddingService = embeddingService;
        _templateResolution = templateResolution;
        _promptBuilder = promptBuilder;
        _imageGeneration = imageGeneration;
        _tracking = tracking;
        _logger = logger;
    }

    public async Task<EnemyGenerationResult> Handle(GenerateEnemyCommand request, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation(
                "Generating enemy: Species={Species}, ThreatLevel={ThreatLevel}, GameSystemId={GameSystemId}",
                request.Species, request.ThreatLevel, request.GameSystemId);

            var templateResult = await _templateResolution.ResolveTemplateAsync(
                request.GameSystemId, request.UserId, "enemy", "Enemy", cancellationToken);

            var (exampleJson, fieldDescriptions) = templateResult.HasTemplate
                ? (templateResult.ExampleJson, templateResult.FieldDescriptions)
                : (templateResult.FallbackExampleJson, templateResult.FallbackFieldDescriptions);

            var ragContext = await GetRagContextAsync(request, cancellationToken);
            var (enemyJson, fullPrompt) = await GenerateJsonAsync(request, ragContext, exampleJson, fieldDescriptions, cancellationToken);

            string? imageBase64 = null;
            string? imageUrl = null;

            if (request.GenerateImage)
            {
                var imageResult = await _imageGeneration.GenerateEntityImageAsync(
                    new ImageGenerationContext(
                        request.GameSystemId,
                        request.UserId,
                        "enemy",
                        $"Terrifying creature concept art, {request.Species}, {request.Behavior} posture, menacing, dark atmosphere, highly detailed, horror aesthetic, professional illustration, dramatic lighting, 8k resolution, NEVER include text.",
                        null,
                        $"Species: {request.Species}, Behavior: {request.Behavior}"),
                    cancellationToken);
                imageBase64 = imageResult.ImageBase64;
                imageUrl = imageResult.ImageUrl;
            }

            var generationRequest = _tracking.CreateRequest(request.UserId, "enemy", fullPrompt);
            generationRequest.Complete();
            var generationResult = _tracking.CreateResult(
                generationRequest.Id, enemyJson, ragContext.Any(), ragContext.Count, imageBase64 != null || imageUrl != null);

            await _tracking.PersistAsync(generationRequest, generationResult, cancellationToken);

            _logger.LogInformation("Enemy generation completed. GenerationRequestId: {RequestId}", generationRequest.Id);

            return new EnemyGenerationResult
            {
                Success = true,
                EntityJson = enemyJson,
                ImageBase64 = imageBase64,
                ImageUrl = imageUrl,
                RagContextUsed = ragContext.Any(),
                RagSourceCount = ragContext.Count,
                GenerationRequestId = generationRequest.Id
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate enemy");
            return new EnemyGenerationResult { Success = false, Error = ex.Message };
        }
    }

    private async Task<IReadOnlyList<RagContextChunk>> GetRagContextAsync(GenerateEnemyCommand request, CancellationToken ct)
    {
        if (!request.GameSystemId.HasValue) return Array.Empty<RagContextChunk>();
        return await _ragContextProvider.GetContextForEntityGenerationAsync(
            request.GameSystemId.Value, request.UserId, "enemy",
            $"Species type: {request.Species}, Threat level: {request.ThreatLevel}, Behavior: {request.Behavior}, Environment: {request.Environment}",
            7, ct);
    }

    private async Task<(string Json, string FullPrompt)> GenerateJsonAsync(
        GenerateEnemyCommand request,
        IReadOnlyList<RagContextChunk> ragContext,
        string exampleJson,
        string fieldDescriptions,
        CancellationToken ct)
    {
        var systemPrompt = @"You are an enemy/creature generator for a tabletop RPG.
Use the provided lore context from the game manuals to create an enemy that fits the game's setting.
The enemy must be consistent with the game system's lore, bestiary, and combat mechanics.
Respond only with valid minified JSON, no markdown code fences.";

        var userQuery = $@"Based on the game system lore provided, generate a hostile creature/enemy with these parameters:
Species Type: {request.Species}
Threat Level: {request.ThreatLevel}
Behavior Pattern: {request.Behavior}
Environment: {request.Environment}

Generate a JSON object with the following fields:
{fieldDescriptions}

The enemy should reflect the game world's creatures, factions, and known threats from the lore.

Example format:
{exampleJson}";

        if (ragContext.Any())
        {
            var contextTexts = ragContext.Select(c => c.Content).ToList();
            var fullPrompt = _promptBuilder.BuildFullPromptTrace(systemPrompt, userQuery, contextTexts);
            var result = await _embeddingService.GenerateWithContextAsync(userQuery, contextTexts, systemPrompt, 0.8f, 2048, ct);
            return (result.Answer, fullPrompt);
        }
        else
        {
            var fallbackSystemPrompt = "You are a monster designer. Create terrifying but balanced enemies. Respond only with valid minified JSON, no markdown code fences.";
            var fallbackPrompt = $@"Create a hostile creature/enemy for a tabletop RPG:
Species Type: {request.Species}
Threat Level: {request.ThreatLevel}
Behavior Pattern: {request.Behavior}
Environment: {request.Environment}

Respond with a JSON object containing the following fields:
{fieldDescriptions}

Example format:
{exampleJson}";

            var fullPrompt = _promptBuilder.BuildFullPromptTrace(fallbackSystemPrompt, fallbackPrompt);
            var result = await _aiService.GenerateJsonAsync(fallbackPrompt, fallbackSystemPrompt, 0.8f, 2048, ct);
            return (result.Json, fullPrompt);
        }
    }
}
