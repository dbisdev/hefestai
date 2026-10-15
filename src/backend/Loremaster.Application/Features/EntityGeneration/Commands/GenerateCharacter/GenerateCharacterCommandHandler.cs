using Loremaster.Application.Common.Interfaces;
using Loremaster.Application.Features.EntityGeneration.Commands.Shared;
using Loremaster.Application.Features.EntityGeneration.Services;
using Loremaster.Domain.Constants;
using Microsoft.Extensions.Logging;

namespace Loremaster.Application.Features.EntityGeneration.Commands.GenerateCharacter;

/// <summary>
/// Handles the <see cref="GenerateCharacterCommand"/> by orchestrating template resolution,
/// RAG-augmented JSON generation, optional portrait generation, and generation tracking.
/// All pipeline logic lives in <see cref="EntityGenerationHandlerBase{TCommand,TResult}"/>;
/// this class provides only character-specific prompts and context.
/// </summary>
public sealed class GenerateCharacterCommandHandler
    : EntityGenerationHandlerBase<GenerateCharacterCommand, CharacterGenerationResult>
{
    /// <inheritdoc/>
    public GenerateCharacterCommandHandler(
        IAiService aiService,
        IRagContextProvider ragContextProvider,
        IRagGenerationService ragGeneration,
        ITemplateResolutionService templateResolution,
        IPromptBuilderService promptBuilder,
        IImageGenerationService imageGeneration,
        IGenerationTrackingService tracking,
        ILogger<GenerateCharacterCommandHandler> logger)
        : base(aiService, ragContextProvider, ragGeneration,
               templateResolution, promptBuilder, imageGeneration, tracking, logger) { }

    // ── Entity metadata ────────────────────────────────────────────────────────

    /// <inheritdoc/>
    protected override string GetEntityType(GenerateCharacterCommand request) => CanonicalEntityTypes.Character;

    /// <inheritdoc/>
    protected override Guid GetUserId(GenerateCharacterCommand request) => request.UserId;

    /// <inheritdoc/>
    protected override Guid? GetGameSystemId(GenerateCharacterCommand request) => request.GameSystemId;

    /// <inheritdoc/>
    protected override bool ShouldGenerateImage(GenerateCharacterCommand request) => request.GenerateImage;

    // ── RAG ────────────────────────────────────────────────────────────────────

    /// <inheritdoc/>
    protected override string BuildRagQuery(GenerateCharacterCommand request) =>
        $"Species: {request.Species}, Role: {request.Role}, Morphology: {request.Morphology}";

    // ── Prompts ────────────────────────────────────────────────────────────────

    /// <inheritdoc/>
    protected override string GetSystemPrompt(GenerateCharacterCommand request) =>
        """
        You are a character generator for a tabletop RPG.
        Use the provided lore context from the game manuals to create a character that fits the game's setting and rules.
        The character must be consistent with the game system's lore, species, classes, and mechanics.
        Respond only with valid minified JSON, no markdown code fences.
        """;

    /// <inheritdoc/>
    protected override string BuildUserQuery(
        GenerateCharacterCommand request, string fieldDescriptions, string exampleJson) =>
        $"""
        Based on the game system lore provided, generate a character with these parameters:
        Species: {request.Species}
        Role: {request.Role}
        Morphology: {request.Morphology}

        Generate a JSON object with the following fields:
        {fieldDescriptions}

        The character should reflect the game world's themes, factions, and available character options from the lore.

        Example format:
        {exampleJson}
        """;

    /// <inheritdoc/>
    protected override string GetFallbackSystemPrompt(GenerateCharacterCommand request) =>
        "You are a sci-fi character generator. Respond only with valid minified JSON, no markdown code fences.";

    /// <inheritdoc/>
    protected override string BuildFallbackPrompt(
        GenerateCharacterCommand request, string fieldDescriptions, string exampleJson) =>
        $"""
        Generate a sci-fi character based on:
        Species: {request.Species}
        Role: {request.Role}
        Morphology: {request.Morphology}

        Respond with a JSON object containing the following fields:
        {fieldDescriptions}

        Example format:
        {exampleJson}
        """;

    // ── Image ──────────────────────────────────────────────────────────────────

    /// <inheritdoc/>
    protected override ImageGenerationContext BuildImageContext(
        GenerateCharacterCommand request, string? entityJson) =>
        new(
            request.GameSystemId,
            request.UserId,
            CanonicalEntityTypes.Character,
            $"High-quality portrait of a {request.Species} {request.Role}, {request.Morphology}. Cinematic lighting, detailed face, 8k resolution, professional concept art, black background, NEVER include text.",
            ExtractDescription(entityJson),
            $"Species: {request.Species}, Role: {request.Role}, Morphology: {request.Morphology}");
}
