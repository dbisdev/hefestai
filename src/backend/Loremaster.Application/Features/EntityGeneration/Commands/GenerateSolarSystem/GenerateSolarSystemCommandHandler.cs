using Loremaster.Application.Common.Interfaces;
using Loremaster.Application.Features.EntityGeneration.Commands.Shared;
using Loremaster.Application.Features.EntityGeneration.Services;
using MediatR;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace Loremaster.Application.Features.EntityGeneration.Commands.GenerateSolarSystem;

public class GenerateSolarSystemCommandHandler : IRequestHandler<GenerateSolarSystemCommand, SolarSystemGenerationResult>
{
    private readonly IAiService _aiService;
    private readonly IRagContextProvider _ragContextProvider;
    private readonly IEmbeddingService _embeddingService;
    private readonly ITemplateResolutionService _templateResolution;
    private readonly IPromptBuilderService _promptBuilder;
    private readonly IImageGenerationService _imageGeneration;
    private readonly IGenerationTrackingService _tracking;
    private readonly ILogger<GenerateSolarSystemCommandHandler> _logger;

    public GenerateSolarSystemCommandHandler(
        IAiService aiService,
        IRagContextProvider ragContextProvider,
        IEmbeddingService embeddingService,
        ITemplateResolutionService templateResolution,
        IPromptBuilderService promptBuilder,
        IImageGenerationService imageGeneration,
        IGenerationTrackingService tracking,
        ILogger<GenerateSolarSystemCommandHandler> logger)
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

    public async Task<SolarSystemGenerationResult> Handle(GenerateSolarSystemCommand request, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation(
                "Generating solar system: SpectralClass={SpectralClass}, Planets={PlanetCount}, GameSystemId={GameSystemId}",
                request.SpectralClass, request.PlanetCount, request.GameSystemId);

            var templateResult = await _templateResolution.ResolveTemplateAsync(
                request.GameSystemId, request.UserId, "solar_system", "Solar System", cancellationToken);

            var (exampleJson, fieldDescriptions) = templateResult.HasTemplate
                ? (templateResult.ExampleJson, templateResult.FieldDescriptions)
                : (templateResult.FallbackExampleJson, templateResult.FallbackFieldDescriptions);

            var ragContext = await GetRagContextAsync(request, cancellationToken);
            var (systemJson, fullPrompt) = await GenerateJsonAsync(request, ragContext, exampleJson, fieldDescriptions, cancellationToken);

            string? imageBase64 = null;
            string? imageUrl = null;

            if (request.GenerateImage)
            {
                var description = ExtractDescription(systemJson);
                var imageResult = await _imageGeneration.GenerateEntityImageAsync(
                    new ImageGenerationContext(
                        request.GameSystemId,
                        request.UserId,
                        "solar-system",
                        $"Breathtaking wide-angle cinematic view of a {request.SpectralClass}-type star solar system. Visible planets orbiting, vibrant cosmic nebulas in background, high detail, photorealistic space photography, sci-fi concept art, deep blacks, vivid colors.",
                        description),
                    cancellationToken);
                imageBase64 = imageResult.ImageBase64;
                imageUrl = imageResult.ImageUrl;
            }

            var generationRequest = _tracking.CreateRequest(request.UserId, "solar-system", fullPrompt);
            generationRequest.Complete();
            var generationResult = _tracking.CreateResult(
                generationRequest.Id, systemJson, ragContext.Any(), ragContext.Count, imageBase64 != null || imageUrl != null);

            await _tracking.PersistAsync(generationRequest, generationResult, cancellationToken);

            _logger.LogInformation("Solar system generation completed. GenerationRequestId: {RequestId}", generationRequest.Id);

            return new SolarSystemGenerationResult
            {
                Success = true,
                EntityJson = systemJson,
                ImageBase64 = imageBase64,
                ImageUrl = imageUrl,
                RagContextUsed = ragContext.Any(),
                RagSourceCount = ragContext.Count,
                GenerationRequestId = generationRequest.Id
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate solar system");
            return new SolarSystemGenerationResult { Success = false, Error = ex.Message };
        }
    }

    private async Task<IReadOnlyList<RagContextChunk>> GetRagContextAsync(GenerateSolarSystemCommand request, CancellationToken ct)
    {
        if (!request.GameSystemId.HasValue) return Array.Empty<RagContextChunk>();
        return await _ragContextProvider.GetContextForEntityGenerationAsync(
            request.GameSystemId.Value, request.UserId, "solar-system",
            $"Star spectral class: {request.SpectralClass}, Planet count: {request.PlanetCount}",
            7, ct);
    }

    private async Task<(string Json, string FullPrompt)> GenerateJsonAsync(
        GenerateSolarSystemCommand request,
        IReadOnlyList<RagContextChunk> ragContext,
        string exampleJson,
        string fieldDescriptions,
        CancellationToken ct)
    {
        var systemPrompt = @"You are a star system generator for a tabletop RPG.
Use the provided lore context from the game manuals to create a star system that fits the game's setting.
The system must be consistent with the game system's lore, factions, and cosmic geography.
Respond only with valid minified JSON, no markdown code fences.";

        var userQuery = $@"Based on the game system lore provided, generate a star system with these parameters:
Star Spectral Class: {request.SpectralClass}
Number of Planets: {request.PlanetCount}

Generate a JSON object with the following fields:
{fieldDescriptions}

The system should reflect the game world's cosmic themes and known regions from the lore.

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
            var fallbackSystemPrompt = "You are a sci-fi world builder. Respond only with valid minified JSON, no markdown code fences.";
            var fallbackPrompt = $@"Create a futuristic star system with {request.PlanetCount} planets orbiting a {request.SpectralClass} class star.
Provide a unique sci-fi name, a brief overview of the system, and a name for each planet.

Respond with a JSON object containing the following fields:
{fieldDescriptions}

Example format:
{exampleJson}";

            var fullPrompt = _promptBuilder.BuildFullPromptTrace(fallbackSystemPrompt, fallbackPrompt);
            var result = await _aiService.GenerateJsonAsync(fallbackPrompt, fallbackSystemPrompt, 0.8f, 2048, ct);
            return (result.Json, fullPrompt);
        }
    }

    private static string? ExtractDescription(string? json)
    {
        if (string.IsNullOrEmpty(json)) return null;
        try
        {
            var cleanedJson = Loremaster.Shared.Helpers.JsonSanitizationHelper.StripMarkdownCodeFences(json);
            using var doc = JsonDocument.Parse(cleanedJson);
            if (doc.RootElement.TryGetProperty("description", out var desc))
                return desc.GetString();
        }
        catch { }
        return null;
    }
}
