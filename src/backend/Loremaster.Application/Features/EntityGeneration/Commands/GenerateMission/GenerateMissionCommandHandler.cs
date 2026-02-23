using Loremaster.Application.Common.Interfaces;
using Loremaster.Application.Features.EntityGeneration.Commands.Shared;
using Loremaster.Application.Features.EntityGeneration.Services;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Loremaster.Application.Features.EntityGeneration.Commands.GenerateMission;

public class GenerateMissionCommandHandler : IRequestHandler<GenerateMissionCommand, MissionGenerationResult>
{
    private readonly IAiService _aiService;
    private readonly IRagContextProvider _ragContextProvider;
    private readonly IEmbeddingService _embeddingService;
    private readonly ITemplateResolutionService _templateResolution;
    private readonly IPromptBuilderService _promptBuilder;
    private readonly IImageGenerationService _imageGeneration;
    private readonly IGenerationTrackingService _tracking;
    private readonly ILogger<GenerateMissionCommandHandler> _logger;

    public GenerateMissionCommandHandler(
        IAiService aiService,
        IRagContextProvider ragContextProvider,
        IEmbeddingService embeddingService,
        ITemplateResolutionService templateResolution,
        IPromptBuilderService promptBuilder,
        IImageGenerationService imageGeneration,
        IGenerationTrackingService tracking,
        ILogger<GenerateMissionCommandHandler> logger)
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

    public async Task<MissionGenerationResult> Handle(GenerateMissionCommand request, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation(
                "Generating mission: Type={MissionType}, Difficulty={Difficulty}, GameSystemId={GameSystemId}",
                request.MissionType, request.Difficulty, request.GameSystemId);

            var templateResult = await _templateResolution.ResolveTemplateAsync(
                request.GameSystemId, request.UserId, "mission", "Mission", cancellationToken);

            var (exampleJson, fieldDescriptions) = templateResult.HasTemplate
                ? (templateResult.ExampleJson, templateResult.FieldDescriptions)
                : (templateResult.FallbackExampleJson, templateResult.FallbackFieldDescriptions);

            var ragContext = await GetRagContextAsync(request, cancellationToken);
            var (missionJson, fullPrompt) = await GenerateJsonAsync(request, ragContext, exampleJson, fieldDescriptions, cancellationToken);

            string? imageBase64 = null;
            string? imageUrl = null;

            if (request.GenerateImage)
            {
                var imageResult = await _imageGeneration.GenerateEntityImageAsync(
                    new ImageGenerationContext(
                        request.GameSystemId,
                        request.UserId,
                        "mission",
                        $"Cinematic scene depicting a {request.MissionType} mission in a {request.Environment}. Dramatic atmosphere, {request.Difficulty} difficulty feel, tactical environment, concept art style, moody lighting, 8k resolution, NEVER include text.",
                        null,
                        $"Mission type: {request.MissionType}, Environment: {request.Environment}, Difficulty: {request.Difficulty}"),
                    cancellationToken);
                imageBase64 = imageResult.ImageBase64;
                imageUrl = imageResult.ImageUrl;
            }

            var generationRequest = _tracking.CreateRequest(request.UserId, "mission", fullPrompt);
            generationRequest.Complete();
            var generationResult = _tracking.CreateResult(
                generationRequest.Id, missionJson, ragContext.Any(), ragContext.Count, imageBase64 != null || imageUrl != null);

            await _tracking.PersistAsync(generationRequest, generationResult, cancellationToken);

            _logger.LogInformation("Mission generation completed. GenerationRequestId: {RequestId}", generationRequest.Id);

            return new MissionGenerationResult
            {
                Success = true,
                EntityJson = missionJson,
                ImageBase64 = imageBase64,
                ImageUrl = imageUrl,
                RagContextUsed = ragContext.Any(),
                RagSourceCount = ragContext.Count,
                GenerationRequestId = generationRequest.Id
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate mission");
            return new MissionGenerationResult { Success = false, Error = ex.Message };
        }
    }

    private async Task<IReadOnlyList<RagContextChunk>> GetRagContextAsync(GenerateMissionCommand request, CancellationToken ct)
    {
        if (!request.GameSystemId.HasValue) return Array.Empty<RagContextChunk>();
        return await _ragContextProvider.GetContextForEntityGenerationAsync(
            request.GameSystemId.Value, request.UserId, "mission",
            $"Mission type: {request.MissionType}, Difficulty: {request.Difficulty}, Environment: {request.Environment}, Faction: {request.FactionInvolved}",
            7, ct);
    }

    private async Task<(string Json, string FullPrompt)> GenerateJsonAsync(
        GenerateMissionCommand request,
        IReadOnlyList<RagContextChunk> ragContext,
        string exampleJson,
        string fieldDescriptions,
        CancellationToken ct)
    {
        var systemPrompt = @"You are a mission/quest generator for a tabletop RPG.
Use the provided lore context from the game manuals to create a mission that fits the game's setting.
The mission must be consistent with the game system's lore, factions, and narrative themes.
Respond only with valid minified JSON, no markdown code fences.";

        var userQuery = $@"Based on the game system lore provided, generate a mission with these parameters:
Mission Type: {request.MissionType}
Difficulty: {request.Difficulty}
Environment: {request.Environment}
Faction Involved: {request.FactionInvolved}

Generate a JSON object with the following fields:
{fieldDescriptions}

The mission should reflect the game world's factions, conflicts, and narrative themes from the lore.

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
            var fallbackSystemPrompt = "You are a mission designer. Create engaging quests with clear objectives. Respond only with valid minified JSON, no markdown code fences.";
            var fallbackPrompt = $@"Create a RPG mission/quest:
Mission Type: {request.MissionType}
Difficulty: {request.Difficulty}
Environment: {request.Environment}
Faction Involved: {request.FactionInvolved}

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
