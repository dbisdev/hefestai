using Loremaster.Application.Common.Interfaces;
using Loremaster.Application.Features.EntityGeneration.Commands.Shared;
using Loremaster.Application.Features.EntityGeneration.Services;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Loremaster.Application.Features.EntityGeneration.Commands.GenerateVehicle;

public class GenerateVehicleCommandHandler : IRequestHandler<GenerateVehicleCommand, VehicleGenerationResult>
{
    private readonly IAiService _aiService;
    private readonly IRagContextProvider _ragContextProvider;
    private readonly IEmbeddingService _embeddingService;
    private readonly ITemplateResolutionService _templateResolution;
    private readonly IPromptBuilderService _promptBuilder;
    private readonly IImageGenerationService _imageGeneration;
    private readonly IGenerationTrackingService _tracking;
    private readonly ILogger<GenerateVehicleCommandHandler> _logger;

    public GenerateVehicleCommandHandler(
        IAiService aiService,
        IRagContextProvider ragContextProvider,
        IEmbeddingService embeddingService,
        ITemplateResolutionService templateResolution,
        IPromptBuilderService promptBuilder,
        IImageGenerationService imageGeneration,
        IGenerationTrackingService tracking,
        ILogger<GenerateVehicleCommandHandler> logger)
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

    public async Task<VehicleGenerationResult> Handle(GenerateVehicleCommand request, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation(
                "Generating vehicle: Type={Type}, Class={Class}, GameSystemId={GameSystemId}",
                request.Type, request.Class, request.GameSystemId);

            var templateResult = await _templateResolution.ResolveTemplateAsync(
                request.GameSystemId, request.UserId, "vehicle", "Vehicle", cancellationToken);

            var (exampleJson, fieldDescriptions) = templateResult.HasTemplate
                ? (templateResult.ExampleJson, templateResult.FieldDescriptions)
                : (templateResult.FallbackExampleJson, templateResult.FallbackFieldDescriptions);

            var ragContext = await GetRagContextAsync(request, cancellationToken);
            var (vehicleJson, fullPrompt) = await GenerateJsonAsync(request, ragContext, exampleJson, fieldDescriptions, cancellationToken);

            string? imageBase64 = null;
            string? imageUrl = null;

            if (request.GenerateImage)
            {
                var imageResult = await _imageGeneration.GenerateEntityImageAsync(
                    new ImageGenerationContext(
                        request.GameSystemId,
                        request.UserId,
                        "vehicle",
                        $"Stunning {request.Type} design, {request.Class} class, powered by {request.Engine} engine. Sleek vehicle, detailed mechanical components, cinematic lighting, concept art style, black space background, 8k resolution."),
                    cancellationToken);
                imageBase64 = imageResult.ImageBase64;
                imageUrl = imageResult.ImageUrl;
            }

            var generationRequest = _tracking.CreateRequest(request.UserId, "vehicle", fullPrompt);
            generationRequest.Complete();
            var generationResult = _tracking.CreateResult(
                generationRequest.Id, vehicleJson, ragContext.Any(), ragContext.Count, imageBase64 != null || imageUrl != null);

            await _tracking.PersistAsync(generationRequest, generationResult, cancellationToken);

            _logger.LogInformation("Vehicle generation completed. GenerationRequestId: {RequestId}", generationRequest.Id);

            return new VehicleGenerationResult
            {
                Success = true,
                EntityJson = vehicleJson,
                ImageBase64 = imageBase64,
                ImageUrl = imageUrl,
                RagContextUsed = ragContext.Any(),
                RagSourceCount = ragContext.Count,
                GenerationRequestId = generationRequest.Id
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate vehicle");
            return new VehicleGenerationResult { Success = false, Error = ex.Message };
        }
    }

    private async Task<IReadOnlyList<RagContextChunk>> GetRagContextAsync(GenerateVehicleCommand request, CancellationToken ct)
    {
        if (!request.GameSystemId.HasValue) return Array.Empty<RagContextChunk>();
        return await _ragContextProvider.GetContextForEntityGenerationAsync(
            request.GameSystemId.Value, request.UserId, "vehicle",
            $"Vehicle type: {request.Type}, Class: {request.Class}, Engine: {request.Engine}",
            7, ct);
    }

    private async Task<(string Json, string FullPrompt)> GenerateJsonAsync(
        GenerateVehicleCommand request,
        IReadOnlyList<RagContextChunk> ragContext,
        string exampleJson,
        string fieldDescriptions,
        CancellationToken ct)
    {
        var systemPrompt = @"You are a vehicle generator for a tabletop RPG.
Use the provided lore context from the game manuals to create a vehicle that fits the game's setting and technology level.
The vehicle must be consistent with the game system's lore, factions, and available technology.
Respond only with valid minified JSON, no markdown code fences.";

        var userQuery = $@"Based on the game system lore provided, generate a vehicle with these parameters:
Type: {request.Type}
Class: {request.Class}
Engine: {request.Engine}

Generate a JSON object with the following fields:
{fieldDescriptions}

The vehicle should reflect the game world's technology and design aesthetics from the lore.

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
            var fallbackSystemPrompt = "You are a vehicle designer. Respond only with valid minified JSON, no markdown code fences.";
            var fallbackPrompt = $@"Create a vehicle:
Type: {request.Type}
Class: {request.Class}
Engine: {request.Engine}

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
