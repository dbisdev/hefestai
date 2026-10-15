using Loremaster.Application.Common.Interfaces;
using Loremaster.Application.Features.EntityGeneration.Commands.Shared;
using Loremaster.Application.Features.EntityGeneration.Services;
using Loremaster.Domain.Constants;
using Microsoft.Extensions.Logging;

namespace Loremaster.Application.Features.EntityGeneration.Commands.GenerateVehicle;

/// <summary>
/// Handles the <see cref="GenerateVehicleCommand"/> by orchestrating template resolution,
/// RAG-augmented JSON generation, optional image generation, and generation tracking.
/// All pipeline logic lives in <see cref="EntityGenerationHandlerBase{TCommand,TResult}"/>;
/// this class provides only vehicle-specific prompts and context.
/// </summary>
public sealed class GenerateVehicleCommandHandler
    : EntityGenerationHandlerBase<GenerateVehicleCommand, VehicleGenerationResult>
{
    /// <inheritdoc/>
    public GenerateVehicleCommandHandler(
        IAiService aiService,
        IRagContextProvider ragContextProvider,
        IRagGenerationService ragGeneration,
        ITemplateResolutionService templateResolution,
        IPromptBuilderService promptBuilder,
        IImageGenerationService imageGeneration,
        IGenerationTrackingService tracking,
        ILogger<GenerateVehicleCommandHandler> logger)
        : base(aiService, ragContextProvider, ragGeneration,
               templateResolution, promptBuilder, imageGeneration, tracking, logger) { }

    // ── Entity metadata ────────────────────────────────────────────────────────

    /// <inheritdoc/>
    protected override string GetEntityType(GenerateVehicleCommand request) => CanonicalEntityTypes.Vehicle;

    /// <inheritdoc/>
    protected override Guid GetUserId(GenerateVehicleCommand request) => request.UserId;

    /// <inheritdoc/>
    protected override Guid? GetGameSystemId(GenerateVehicleCommand request) => request.GameSystemId;

    /// <inheritdoc/>
    protected override bool ShouldGenerateImage(GenerateVehicleCommand request) => request.GenerateImage;

    // ── RAG ────────────────────────────────────────────────────────────────────

    /// <inheritdoc/>
    protected override string BuildRagQuery(GenerateVehicleCommand request) =>
        $"Vehicle type: {request.Type}, Class: {request.Class}, Engine: {request.Engine}";

    // ── Prompts ────────────────────────────────────────────────────────────────

    /// <inheritdoc/>
    protected override string GetSystemPrompt(GenerateVehicleCommand request) =>
        """
        You are a vehicle generator for a tabletop RPG.
        Use the provided lore context from the game manuals to create a vehicle that fits the game's setting and technology level.
        The vehicle must be consistent with the game system's lore, factions, and available technology.
        Respond only with valid minified JSON, no markdown code fences.
        """;

    /// <inheritdoc/>
    protected override string BuildUserQuery(
        GenerateVehicleCommand request, string fieldDescriptions, string exampleJson) =>
        $"""
        Based on the game system lore provided, generate a vehicle with these parameters:
        Type: {request.Type}
        Class: {request.Class}
        Engine: {request.Engine}

        Generate a JSON object with the following fields:
        {fieldDescriptions}

        The vehicle should reflect the game world's technology and design aesthetics from the lore.

        Example format:
        {exampleJson}
        """;

    /// <inheritdoc/>
    protected override string GetFallbackSystemPrompt(GenerateVehicleCommand request) =>
        "You are a vehicle designer. Respond only with valid minified JSON, no markdown code fences.";

    /// <inheritdoc/>
    protected override string BuildFallbackPrompt(
        GenerateVehicleCommand request, string fieldDescriptions, string exampleJson) =>
        $"""
        Create a vehicle:
        Type: {request.Type}
        Class: {request.Class}
        Engine: {request.Engine}

        Respond with a JSON object containing the following fields:
        {fieldDescriptions}

        Example format:
        {exampleJson}
        """;

    // ── Image ──────────────────────────────────────────────────────────────────

    /// <inheritdoc/>
    protected override ImageGenerationContext BuildImageContext(
        GenerateVehicleCommand request, string? entityJson) =>
        new(
            request.GameSystemId,
            request.UserId,
            CanonicalEntityTypes.Vehicle,
            $"Stunning {request.Type} design, {request.Class} class. Sleek vehicle, detailed mechanical components, cinematic lighting, concept art style, black background, 8k resolution, NEVER include text.",
            null,
            $"Type: {request.Type}, Class: {request.Class}, Engine: {request.Engine}");
}
