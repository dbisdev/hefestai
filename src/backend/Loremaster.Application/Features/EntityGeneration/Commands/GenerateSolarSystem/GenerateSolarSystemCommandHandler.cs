using Loremaster.Application.Common.Interfaces;
using Loremaster.Application.Features.EntityGeneration.Commands.Shared;
using Loremaster.Application.Features.EntityGeneration.Services;
using Microsoft.Extensions.Logging;

namespace Loremaster.Application.Features.EntityGeneration.Commands.GenerateSolarSystem;

/// <summary>
/// Handles the <see cref="GenerateSolarSystemCommand"/> by orchestrating template resolution,
/// RAG-augmented JSON generation, optional image generation, and generation tracking.
/// All pipeline logic lives in <see cref="EntityGenerationHandlerBase{TCommand,TResult}"/>;
/// this class provides only solar-system-specific prompts and context.
/// </summary>
public sealed class GenerateSolarSystemCommandHandler
    : EntityGenerationHandlerBase<GenerateSolarSystemCommand, SolarSystemGenerationResult>
{
    /// <inheritdoc/>
    public GenerateSolarSystemCommandHandler(
        IAiService aiService,
        IRagContextProvider ragContextProvider,
        IRagGenerationService ragGeneration,
        ITemplateResolutionService templateResolution,
        IPromptBuilderService promptBuilder,
        IImageGenerationService imageGeneration,
        IGenerationTrackingService tracking,
        ILogger<GenerateSolarSystemCommandHandler> logger)
        : base(aiService, ragContextProvider, ragGeneration,
               templateResolution, promptBuilder, imageGeneration, tracking, logger) { }

    // ── Entity metadata ────────────────────────────────────────────────────────

    /// <inheritdoc/>
    /// <remarks>
    /// The entity type uses a hyphen (<c>"solar-system"</c>) to match the template
    /// slug convention used elsewhere in the domain (e.g. image generation context).
    /// </remarks>
    protected override string GetEntityType(GenerateSolarSystemCommand request) => "solar-system";

    /// <inheritdoc/>
    /// <remarks>
    /// Template records in the database use an underscore slug (<c>"solar_system"</c>),
    /// while all other services use the hyphenated form.
    /// </remarks>
    protected override string GetTemplateEntityType(GenerateSolarSystemCommand request) => "solar_system";

    /// <inheritdoc/>
    protected override string GetEntityLabel(GenerateSolarSystemCommand request) => "Solar System";

    /// <inheritdoc/>
    protected override Guid GetUserId(GenerateSolarSystemCommand request) => request.UserId;

    /// <inheritdoc/>
    protected override Guid? GetGameSystemId(GenerateSolarSystemCommand request) => request.GameSystemId;

    /// <inheritdoc/>
    protected override bool ShouldGenerateImage(GenerateSolarSystemCommand request) => request.GenerateImage;

    // ── RAG ────────────────────────────────────────────────────────────────────

    /// <inheritdoc/>
    protected override string BuildRagQuery(GenerateSolarSystemCommand request) =>
        $"Star spectral class: {request.SpectralClass}, Planet count: {request.PlanetCount}";

    // ── Prompts ────────────────────────────────────────────────────────────────

    /// <inheritdoc/>
    protected override string GetSystemPrompt(GenerateSolarSystemCommand request) =>
        """
        You are a star system generator for a tabletop RPG.
        Use the provided lore context from the game manuals to create a star system that fits the game's setting.
        The system must be consistent with the game system's lore, factions, and cosmic geography.
        Respond only with valid minified JSON, no markdown code fences.
        """;

    /// <inheritdoc/>
    protected override string BuildUserQuery(
        GenerateSolarSystemCommand request, string fieldDescriptions, string exampleJson) =>
        $"""
        Based on the game system lore provided, generate a star system with these parameters:
        Star Spectral Class: {request.SpectralClass}
        Number of Planets: {request.PlanetCount}

        Generate a JSON object with the following fields:
        {fieldDescriptions}

        The system should reflect the game world's cosmic themes and known regions from the lore.

        Example format:
        {exampleJson}
        """;

    /// <inheritdoc/>
    protected override string GetFallbackSystemPrompt(GenerateSolarSystemCommand request) =>
        "You are a sci-fi world builder. Respond only with valid minified JSON, no markdown code fences.";

    /// <inheritdoc/>
    protected override string BuildFallbackPrompt(
        GenerateSolarSystemCommand request, string fieldDescriptions, string exampleJson) =>
        $"""
        Create a futuristic star system with {request.PlanetCount} planets orbiting a {request.SpectralClass} class star.
        Provide a unique sci-fi name, a brief overview of the system, and a name for each planet.

        Respond with a JSON object containing the following fields:
        {fieldDescriptions}

        Example format:
        {exampleJson}
        """;

    // ── Image ──────────────────────────────────────────────────────────────────

    /// <inheritdoc/>
    protected override ImageGenerationContext BuildImageContext(
        GenerateSolarSystemCommand request, string? entityJson) =>
        new(
            request.GameSystemId,
            request.UserId,
            "solar-system",
            $"Breathtaking wide-angle cinematic view of a {request.SpectralClass}-type star solar system. Visible planets orbiting, vibrant cosmic nebulas in background, high detail, photorealistic space photography, sci-fi concept art, deep blacks, vivid colors.",
            ExtractDescription(entityJson),
            $"Spectral class: {request.SpectralClass}");
}
