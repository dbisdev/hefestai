using Loremaster.Application.Common.Interfaces;
using Loremaster.Application.Features.EntityGeneration.Commands.Shared;
using Loremaster.Application.Features.EntityGeneration.Services;
using Microsoft.Extensions.Logging;

namespace Loremaster.Application.Features.EntityGeneration.Commands.GenerateEnemy;

/// <summary>
/// Handles the <see cref="GenerateEnemyCommand"/> by orchestrating template resolution,
/// RAG-augmented JSON generation, optional image generation, and generation tracking.
/// All pipeline logic lives in <see cref="EntityGenerationHandlerBase{TCommand,TResult}"/>;
/// this class provides only enemy-specific prompts and context.
/// </summary>
public sealed class GenerateEnemyCommandHandler
    : EntityGenerationHandlerBase<GenerateEnemyCommand, EnemyGenerationResult>
{
    /// <inheritdoc/>
    public GenerateEnemyCommandHandler(
        IAiService aiService,
        IRagContextProvider ragContextProvider,
        IRagGenerationService ragGeneration,
        ITemplateResolutionService templateResolution,
        IPromptBuilderService promptBuilder,
        IImageGenerationService imageGeneration,
        IGenerationTrackingService tracking,
        ILogger<GenerateEnemyCommandHandler> logger)
        : base(aiService, ragContextProvider, ragGeneration,
               templateResolution, promptBuilder, imageGeneration, tracking, logger) { }

    // ── Entity metadata ────────────────────────────────────────────────────────

    /// <inheritdoc/>
    protected override string GetEntityType(GenerateEnemyCommand request) => "enemy";

    /// <inheritdoc/>
    protected override Guid GetUserId(GenerateEnemyCommand request) => request.UserId;

    /// <inheritdoc/>
    protected override Guid? GetGameSystemId(GenerateEnemyCommand request) => request.GameSystemId;

    /// <inheritdoc/>
    protected override bool ShouldGenerateImage(GenerateEnemyCommand request) => request.GenerateImage;

    // ── RAG ────────────────────────────────────────────────────────────────────

    /// <inheritdoc/>
    protected override string BuildRagQuery(GenerateEnemyCommand request) =>
        $"Species type: {request.Species}, Threat level: {request.ThreatLevel}, Behavior: {request.Behavior}, Environment: {request.Environment}";

    // ── Prompts ────────────────────────────────────────────────────────────────

    /// <inheritdoc/>
    protected override string GetSystemPrompt(GenerateEnemyCommand request) =>
        """
        You are an enemy/creature generator for a tabletop RPG.
        Use the provided lore context from the game manuals to create an enemy that fits the game's setting.
        The enemy must be consistent with the game system's lore, bestiary, and combat mechanics.
        Respond only with valid minified JSON, no markdown code fences.
        """;

    /// <inheritdoc/>
    protected override string BuildUserQuery(
        GenerateEnemyCommand request, string fieldDescriptions, string exampleJson) =>
        $"""
        Based on the game system lore provided, generate a hostile creature/enemy with these parameters:
        Species Type: {request.Species}
        Threat Level: {request.ThreatLevel}
        Behavior Pattern: {request.Behavior}
        Environment: {request.Environment}

        Generate a JSON object with the following fields:
        {fieldDescriptions}

        The enemy should reflect the game world's creatures, factions, and known threats from the lore.

        Example format:
        {exampleJson}
        """;

    /// <inheritdoc/>
    protected override string GetFallbackSystemPrompt(GenerateEnemyCommand request) =>
        "You are a monster designer. Create terrifying but balanced enemies. Respond only with valid minified JSON, no markdown code fences.";

    /// <inheritdoc/>
    protected override string BuildFallbackPrompt(
        GenerateEnemyCommand request, string fieldDescriptions, string exampleJson) =>
        $"""
        Create a hostile creature/enemy for a tabletop RPG:
        Species Type: {request.Species}
        Threat Level: {request.ThreatLevel}
        Behavior Pattern: {request.Behavior}
        Environment: {request.Environment}

        Respond with a JSON object containing the following fields:
        {fieldDescriptions}

        Example format:
        {exampleJson}
        """;

    // ── Image ──────────────────────────────────────────────────────────────────

    /// <inheritdoc/>
    protected override ImageGenerationContext BuildImageContext(
        GenerateEnemyCommand request, string? entityJson) =>
        new(
            request.GameSystemId,
            request.UserId,
            "enemy",
            $"Terrifying creature concept art, {request.Species}, {request.Behavior} posture, menacing, dark atmosphere, highly detailed, horror aesthetic, professional illustration, dramatic lighting, 8k resolution, NEVER include text.",
            null,
            $"Species: {request.Species}, Behavior: {request.Behavior}");
}
