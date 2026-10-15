using Loremaster.Application.Common.Interfaces;
using Loremaster.Application.Features.EntityGeneration.Commands.Shared;
using Loremaster.Application.Features.EntityGeneration.Services;
using Microsoft.Extensions.Logging;

namespace Loremaster.Application.Features.EntityGeneration.Commands.GenerateEncounter;

/// <summary>
/// Handles the <see cref="GenerateEncounterCommand"/> by orchestrating template resolution,
/// RAG-augmented JSON generation, optional scene image generation, and generation tracking.
/// All pipeline logic lives in <see cref="EntityGenerationHandlerBase{TCommand,TResult}"/>;
/// this class provides only encounter-specific prompts and context.
/// </summary>
public sealed class GenerateEncounterCommandHandler
    : EntityGenerationHandlerBase<GenerateEncounterCommand, EncounterGenerationResult>
{
    /// <inheritdoc/>
    public GenerateEncounterCommandHandler(
        IAiService aiService,
        IRagContextProvider ragContextProvider,
        IRagGenerationService ragGeneration,
        ITemplateResolutionService templateResolution,
        IPromptBuilderService promptBuilder,
        IImageGenerationService imageGeneration,
        IGenerationTrackingService tracking,
        ILogger<GenerateEncounterCommandHandler> logger)
        : base(aiService, ragContextProvider, ragGeneration,
               templateResolution, promptBuilder, imageGeneration, tracking, logger) { }

    // ── Entity metadata ────────────────────────────────────────────────────────

    /// <inheritdoc/>
    protected override string GetEntityType(GenerateEncounterCommand request) => "encounter";

    /// <inheritdoc/>
    protected override Guid GetUserId(GenerateEncounterCommand request) => request.UserId;

    /// <inheritdoc/>
    protected override Guid? GetGameSystemId(GenerateEncounterCommand request) => request.GameSystemId;

    /// <inheritdoc/>
    protected override bool ShouldGenerateImage(GenerateEncounterCommand request) => request.GenerateImage;

    // ── RAG ────────────────────────────────────────────────────────────────────

    /// <inheritdoc/>
    protected override string BuildRagQuery(GenerateEncounterCommand request) =>
        $"Encounter type: {request.EncounterType}, Difficulty: {request.Difficulty}, Environment: {request.Environment}, Enemy count: {request.EnemyCount}";

    // ── Prompts ────────────────────────────────────────────────────────────────

    /// <inheritdoc/>
    protected override string GetSystemPrompt(GenerateEncounterCommand request) =>
        """
        You are a combat encounter generator for a tabletop RPG.
        Use the provided lore context from the game manuals to create an encounter that fits the game's setting.
        The encounter must be consistent with the game system's lore, combat rules, and enemy types.
        Respond only with valid minified JSON, no markdown code fences.
        """;

    /// <inheritdoc/>
    protected override string BuildUserQuery(
        GenerateEncounterCommand request, string fieldDescriptions, string exampleJson) =>
        $"""
        Based on the game system lore provided, generate a combat encounter with these parameters:
        Encounter Type: {request.EncounterType}
        Difficulty: {request.Difficulty}
        Environment: {request.Environment}
        Enemy Count: {request.EnemyCount}

        Generate a JSON object with the following fields:
        {fieldDescriptions}

        The encounter should reflect the game world's enemies, factions, and combat scenarios from the lore.

        Example format:
        {exampleJson}
        """;

    /// <inheritdoc/>
    protected override string GetFallbackSystemPrompt(GenerateEncounterCommand request) =>
        "You are an encounter designer. Create tactical and exciting combat scenarios. Respond only with valid minified JSON, no markdown code fences.";

    /// <inheritdoc/>
    protected override string BuildFallbackPrompt(
        GenerateEncounterCommand request, string fieldDescriptions, string exampleJson) =>
        $"""
        Create a RPG combat/tactical encounter:
        Encounter Type: {request.EncounterType}
        Difficulty: {request.Difficulty}
        Environment: {request.Environment}
        Enemy Count: {request.EnemyCount}

        Respond with a JSON object containing the following fields:
        {fieldDescriptions}

        Example format:
        {exampleJson}
        """;

    // ── Image ──────────────────────────────────────────────────────────────────

    /// <inheritdoc/>
    protected override ImageGenerationContext BuildImageContext(
        GenerateEncounterCommand request, string? entityJson) =>
        new(
            request.GameSystemId,
            request.UserId,
            "encounter",
            $"Intense {request.EncounterType} encounter in a {request.Environment} environment. {request.EnemyCount} enemies, tactical combat scene, dramatic lighting, action-packed atmosphere, concept art style, 8k resolution, NEVER include text.",
            null,
            $"Encounter type: {request.EncounterType}, Environment: {request.Environment}, Enemies: {request.EnemyCount}");
}
