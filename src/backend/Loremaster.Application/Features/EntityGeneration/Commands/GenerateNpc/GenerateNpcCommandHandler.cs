using Loremaster.Application.Common.Interfaces;
using Loremaster.Application.Features.EntityGeneration.Commands.Shared;
using Loremaster.Application.Features.EntityGeneration.Services;
using Loremaster.Domain.Constants;
using Microsoft.Extensions.Logging;

namespace Loremaster.Application.Features.EntityGeneration.Commands.GenerateNpc;

/// <summary>
/// Handles the <see cref="GenerateNpcCommand"/> by orchestrating template resolution,
/// RAG-augmented JSON generation, optional portrait generation, and generation tracking.
/// All pipeline logic lives in <see cref="EntityGenerationHandlerBase{TCommand,TResult}"/>;
/// this class provides only NPC-specific prompts and context.
/// </summary>
/// <remarks>
/// Note: internally the entity type is <c>"actor"</c> (domain terminology),
/// whereas the public API surface uses the term <c>"npc"</c>.
/// </remarks>
public sealed class GenerateNpcCommandHandler
    : EntityGenerationHandlerBase<GenerateNpcCommand, NpcGenerationResult>
{
    /// <inheritdoc/>
    public GenerateNpcCommandHandler(
        IAiService aiService,
        IRagContextProvider ragContextProvider,
        IRagGenerationService ragGeneration,
        ITemplateResolutionService templateResolution,
        IPromptBuilderService promptBuilder,
        IImageGenerationService imageGeneration,
        IGenerationTrackingService tracking,
        ILogger<GenerateNpcCommandHandler> logger)
        : base(aiService, ragContextProvider, ragGeneration,
               templateResolution, promptBuilder, imageGeneration, tracking, logger) { }

    // ── Entity metadata ────────────────────────────────────────────────────────

    /// <inheritdoc/>
    /// <remarks>
    /// The internal domain type is <c>"actor"</c>; the public API calls it <c>"npc"</c>.
    /// Template resolution, RAG and tracking all use the internal name.
    /// </remarks>
    protected override string GetEntityType(GenerateNpcCommand request) => CanonicalEntityTypes.Actor;

    /// <inheritdoc/>
    protected override string GetEntityLabel(GenerateNpcCommand request) => "NPC";

    /// <inheritdoc/>
    protected override Guid GetUserId(GenerateNpcCommand request) => request.UserId;

    /// <inheritdoc/>
    protected override Guid? GetGameSystemId(GenerateNpcCommand request) => request.GameSystemId;

    /// <inheritdoc/>
    protected override bool ShouldGenerateImage(GenerateNpcCommand request) => request.GenerateImage;

    // ── RAG ────────────────────────────────────────────────────────────────────

    /// <inheritdoc/>
    protected override string BuildRagQuery(GenerateNpcCommand request) =>
        $"Species: {request.Species}, Occupation: {request.Occupation}, Personality: {request.Personality}, Setting: {request.Setting}";

    // ── Prompts ────────────────────────────────────────────────────────────────

    /// <inheritdoc/>
    protected override string GetSystemPrompt(GenerateNpcCommand request) =>
        """
        You are an NPC generator for a tabletop RPG.
        Use the provided lore context from the game manuals to create an NPC that fits the game's setting.
        The NPC must be consistent with the game system's lore, factions, species, and social structures.
        Respond only with valid minified JSON, no markdown code fences.
        """;

    /// <inheritdoc/>
    protected override string BuildUserQuery(
        GenerateNpcCommand request, string fieldDescriptions, string exampleJson) =>
        $"""
        Based on the game system lore provided, generate an NPC with these parameters:
        Species: {request.Species}
        Occupation: {request.Occupation}
        Personality: {request.Personality}

        Generate a JSON object with the following fields:
        {fieldDescriptions}

        The NPC should reflect the game world's factions, cultures, and social dynamics from the lore.

        Example format:
        {exampleJson}
        """;

    /// <inheritdoc/>
    protected override string GetFallbackSystemPrompt(GenerateNpcCommand request) =>
        "You are a RPG character creator. Create interesting NPCs with depth. Respond only with valid minified JSON, no markdown code fences.";

    /// <inheritdoc/>
    protected override string BuildFallbackPrompt(
        GenerateNpcCommand request, string fieldDescriptions, string exampleJson) =>
        $"""
        Create an NPC (non-player character) for a tabletop RPG:
        Species: {request.Species}
        Occupation: {request.Occupation}
        Personality: {request.Personality}

        Respond with a JSON object containing the following fields:
        {fieldDescriptions}

        Example format:
        {exampleJson}
        """;

    // ── Image ──────────────────────────────────────────────────────────────────

    /// <inheritdoc/>
    protected override ImageGenerationContext BuildImageContext(
        GenerateNpcCommand request, string? entityJson) =>
        new(
            request.GameSystemId,
            request.UserId,
            CanonicalEntityTypes.Actor,
            $"High-quality portrait of a {request.Species} {request.Occupation}, {request.Personality} expression. Cinematic lighting, detailed face, professional concept art, black background, 8k resolution, NEVER include text.",
            null,
            $"Species: {request.Species}, Occupation: {request.Occupation}, Personality: {request.Personality}");
}
