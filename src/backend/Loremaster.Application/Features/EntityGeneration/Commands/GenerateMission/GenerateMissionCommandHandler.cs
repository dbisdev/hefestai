using Loremaster.Application.Common.Interfaces;
using Loremaster.Application.Features.EntityGeneration.Commands.Shared;
using Loremaster.Application.Features.EntityGeneration.Services;
using Microsoft.Extensions.Logging;

namespace Loremaster.Application.Features.EntityGeneration.Commands.GenerateMission;

/// <summary>
/// Handles the <see cref="GenerateMissionCommand"/> by orchestrating template resolution,
/// RAG-augmented JSON generation, optional scene image generation, and generation tracking.
/// All pipeline logic lives in <see cref="EntityGenerationHandlerBase{TCommand,TResult}"/>;
/// this class provides only mission-specific prompts and context.
/// </summary>
public sealed class GenerateMissionCommandHandler
    : EntityGenerationHandlerBase<GenerateMissionCommand, MissionGenerationResult>
{
    /// <inheritdoc/>
    public GenerateMissionCommandHandler(
        IAiService aiService,
        IRagContextProvider ragContextProvider,
        IRagGenerationService ragGeneration,
        ITemplateResolutionService templateResolution,
        IPromptBuilderService promptBuilder,
        IImageGenerationService imageGeneration,
        IGenerationTrackingService tracking,
        ILogger<GenerateMissionCommandHandler> logger)
        : base(aiService, ragContextProvider, ragGeneration,
               templateResolution, promptBuilder, imageGeneration, tracking, logger) { }

    // ── Entity metadata ────────────────────────────────────────────────────────

    /// <inheritdoc/>
    protected override string GetEntityType(GenerateMissionCommand request) => "mission";

    /// <inheritdoc/>
    protected override Guid GetUserId(GenerateMissionCommand request) => request.UserId;

    /// <inheritdoc/>
    protected override Guid? GetGameSystemId(GenerateMissionCommand request) => request.GameSystemId;

    /// <inheritdoc/>
    protected override bool ShouldGenerateImage(GenerateMissionCommand request) => request.GenerateImage;

    // ── RAG ────────────────────────────────────────────────────────────────────

    /// <inheritdoc/>
    protected override string BuildRagQuery(GenerateMissionCommand request) =>
        $"Mission type: {request.MissionType}, Difficulty: {request.Difficulty}, Environment: {request.Environment}, Faction: {request.FactionInvolved}";

    // ── Prompts ────────────────────────────────────────────────────────────────

    /// <inheritdoc/>
    protected override string GetSystemPrompt(GenerateMissionCommand request) =>
        """
        You are a mission/quest generator for a tabletop RPG.
        Use the provided lore context from the game manuals to create a mission that fits the game's setting.
        The mission must be consistent with the game system's lore, factions, and narrative themes.
        Respond only with valid minified JSON, no markdown code fences.
        """;

    /// <inheritdoc/>
    protected override string BuildUserQuery(
        GenerateMissionCommand request, string fieldDescriptions, string exampleJson) =>
        $"""
        Based on the game system lore provided, generate a mission with these parameters:
        Mission Type: {request.MissionType}
        Difficulty: {request.Difficulty}
        Environment: {request.Environment}
        Faction Involved: {request.FactionInvolved}

        Generate a JSON object with the following fields:
        {fieldDescriptions}

        The mission should reflect the game world's factions, conflicts, and narrative themes from the lore.

        Example format:
        {exampleJson}
        """;

    /// <inheritdoc/>
    protected override string GetFallbackSystemPrompt(GenerateMissionCommand request) =>
        "You are a mission designer. Create engaging quests with clear objectives. Respond only with valid minified JSON, no markdown code fences.";

    /// <inheritdoc/>
    protected override string BuildFallbackPrompt(
        GenerateMissionCommand request, string fieldDescriptions, string exampleJson) =>
        $"""
        Create a RPG mission/quest:
        Mission Type: {request.MissionType}
        Difficulty: {request.Difficulty}
        Environment: {request.Environment}
        Faction Involved: {request.FactionInvolved}

        Respond with a JSON object containing the following fields:
        {fieldDescriptions}

        Example format:
        {exampleJson}
        """;

    // ── Image ──────────────────────────────────────────────────────────────────

    /// <inheritdoc/>
    protected override ImageGenerationContext BuildImageContext(
        GenerateMissionCommand request, string? entityJson) =>
        new(
            request.GameSystemId,
            request.UserId,
            "mission",
            $"Cinematic scene depicting a {request.MissionType} mission in a {request.Environment}. Dramatic atmosphere, {request.Difficulty} difficulty feel, tactical environment, concept art style, moody lighting, 8k resolution, NEVER include text.",
            null,
            $"Mission type: {request.MissionType}, Environment: {request.Environment}, Difficulty: {request.Difficulty}");
}
