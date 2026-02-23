using Loremaster.Application.Common.Interfaces;
using Loremaster.Application.Features.EntityGeneration.Commands.Shared;
using Loremaster.Application.Features.EntityGeneration.Services;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Loremaster.Application.Features.EntityGeneration.Commands.GenerateEncounter;

public class GenerateEncounterCommandHandler : IRequestHandler<GenerateEncounterCommand, EncounterGenerationResult>
{
    private readonly IAiService _aiService;
    private readonly IRagContextProvider _ragContextProvider;
    private readonly IEmbeddingService _embeddingService;
    private readonly ITemplateResolutionService _templateResolution;
    private readonly IPromptBuilderService _promptBuilder;
    private readonly IImageGenerationService _imageGeneration;
    private readonly IGenerationTrackingService _tracking;
    private readonly ILogger<GenerateEncounterCommandHandler> _logger;

    public GenerateEncounterCommandHandler(
        IAiService aiService,
        IRagContextProvider ragContextProvider,
        IEmbeddingService embeddingService,
        ITemplateResolutionService templateResolution,
        IPromptBuilderService promptBuilder,
        IImageGenerationService imageGeneration,
        IGenerationTrackingService tracking,
        ILogger<GenerateEncounterCommandHandler> logger)
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

    public async Task<EncounterGenerationResult> Handle(GenerateEncounterCommand request, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation(
                "Generating encounter: Type={EncounterType}, Difficulty={Difficulty}, GameSystemId={GameSystemId}",
                request.EncounterType, request.Difficulty, request.GameSystemId);

            var templateResult = await _templateResolution.ResolveTemplateAsync(
                request.GameSystemId, request.UserId, "encounter", "Encounter", cancellationToken);

            var (exampleJson, fieldDescriptions) = templateResult.HasTemplate
                ? (templateResult.ExampleJson, templateResult.FieldDescriptions)
                : (templateResult.FallbackExampleJson, templateResult.FallbackFieldDescriptions);

            var ragContext = await GetRagContextAsync(request, cancellationToken);
            var (encounterJson, fullPrompt) = await GenerateJsonAsync(request, ragContext, exampleJson, fieldDescriptions, cancellationToken);

            string? imageBase64 = null;
            string? imageUrl = null;

            if (request.GenerateImage)
            {
                var imageResult = await _imageGeneration.GenerateEntityImageAsync(
                    new ImageGenerationContext(
                        request.GameSystemId,
                        request.UserId,
                        "encounter",
                        $"Intense {request.EncounterType} encounter in a {request.Environment} environment. {request.EnemyCount} enemies, tactical combat scene, dramatic lighting, action-packed atmosphere, concept art style, 8k resolution, NEVER include text.",
                        null,
                        $"Encounter type: {request.EncounterType}, Environment: {request.Environment}, Enemies: {request.EnemyCount}"),
                    cancellationToken);
                imageBase64 = imageResult.ImageBase64;
                imageUrl = imageResult.ImageUrl;
            }

            var generationRequest = _tracking.CreateRequest(request.UserId, "encounter", fullPrompt);
            generationRequest.Complete();
            var generationResult = _tracking.CreateResult(
                generationRequest.Id, encounterJson, ragContext.Any(), ragContext.Count, imageBase64 != null || imageUrl != null);

            await _tracking.PersistAsync(generationRequest, generationResult, cancellationToken);

            _logger.LogInformation("Encounter generation completed. GenerationRequestId: {RequestId}", generationRequest.Id);

            return new EncounterGenerationResult
            {
                Success = true,
                EntityJson = encounterJson,
                ImageBase64 = imageBase64,
                ImageUrl = imageUrl,
                RagContextUsed = ragContext.Any(),
                RagSourceCount = ragContext.Count,
                GenerationRequestId = generationRequest.Id
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate encounter");
            return new EncounterGenerationResult { Success = false, Error = ex.Message };
        }
    }

    private async Task<IReadOnlyList<RagContextChunk>> GetRagContextAsync(GenerateEncounterCommand request, CancellationToken ct)
    {
        if (!request.GameSystemId.HasValue) return Array.Empty<RagContextChunk>();
        return await _ragContextProvider.GetContextForEntityGenerationAsync(
            request.GameSystemId.Value, request.UserId, "encounter",
            $"Encounter type: {request.EncounterType}, Difficulty: {request.Difficulty}, Environment: {request.Environment}, Enemy count: {request.EnemyCount}",
            7, ct);
    }

    private async Task<(string Json, string FullPrompt)> GenerateJsonAsync(
        GenerateEncounterCommand request,
        IReadOnlyList<RagContextChunk> ragContext,
        string exampleJson,
        string fieldDescriptions,
        CancellationToken ct)
    {
        var systemPrompt = @"You are a combat encounter generator for a tabletop RPG.
Use the provided lore context from the game manuals to create an encounter that fits the game's setting.
The encounter must be consistent with the game system's lore, combat rules, and enemy types.
Respond only with valid minified JSON, no markdown code fences.";

        var userQuery = $@"Based on the game system lore provided, generate a combat encounter with these parameters:
Encounter Type: {request.EncounterType}
Difficulty: {request.Difficulty}
Environment: {request.Environment}
Enemy Count: {request.EnemyCount}

Generate a JSON object with the following fields:
{fieldDescriptions}

The encounter should reflect the game world's enemies, factions, and combat scenarios from the lore.

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
            var fallbackSystemPrompt = "You are an encounter designer. Create tactical and exciting combat scenarios. Respond only with valid minified JSON, no markdown code fences.";
            var fallbackPrompt = $@"Create a RPG combat/tactical encounter:
Encounter Type: {request.EncounterType}
Difficulty: {request.Difficulty}
Environment: {request.Environment}
Enemy Count: {request.EnemyCount}

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
