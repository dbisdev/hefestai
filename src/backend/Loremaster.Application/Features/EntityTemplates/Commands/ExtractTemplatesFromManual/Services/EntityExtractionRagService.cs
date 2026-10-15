using Loremaster.Application.Common.Interfaces;
using Microsoft.Extensions.Logging;

namespace Loremaster.Application.Features.EntityTemplates.Commands.ExtractTemplatesFromManual.Services;

/// <summary>
/// Implements <see cref="IEntityExtractionRagService"/> by running multiple semantic searches
/// over game-manual documents and delegating RAG generation to <see cref="IRagGenerationService"/>.
/// </summary>
internal sealed class EntityExtractionRagService : IEntityExtractionRagService
{
    // ── RAG prompt ─────────────────────────────────────────────────────────────

    /// <summary>
    /// System prompt used when calling the LLM for entity-type extraction.
    /// </summary>
    private const string SystemPrompt =
        "You are a game system analyst. Extract ALL entity types: characters, NPCs, vehicles, " +
        "starships, monsters, locations, items. Respond only with valid minified JSON, no markdown code fences.";

    /// <summary>
    /// User query prompt for entity-type extraction.
    /// NOTE: Genkit API limit is 2 000 chars for the query — keep this concise.
    /// </summary>
    private const string ExtractionPrompt =
        """
        Extract ALL entity types from this RPG rulebook (characters, NPCs, vehicles, starships, monsters).

        IMPORTANT: Always use English for 'entityTypeName' field, regardless of manual language.
        Prefer these canonical names when applicable:
        - "character" (player characters)
        - "actor" (NPCs, enemies, allies, contacts)
        - "vehicle" (starships, aircraft, mechs)
        - "monster" (creatures, beasts, aliens)

        Important search ATTRIBUTES and based or dependant SKILLS.
        Return JSON array with nested field structure:
        ```json
        [{
          "entityTypeName": "character",
          "displayName": "Player Character",
          "description": "Playable character",
          "schema": {
            "attributes": {
              "strength": {"name":"Strength","description":"Physical power","range":{"min":1,"max":5}},
              "agility": {"name":"Agility","description":"Speed and reflexes","range":{"min":1,"max":5}}
            },
            "skills": {
              "combat": {"attribute":"strength","description":"Fighting ability"},
              "piloting": {"attribute":"agility","description":"Vehicle control"}
            },
            "derived_stats": {
              "health": {"formula":"strength*2","description":"Hit points"}
            },
            "identity": {
              "name":"string","career":"string","appearance":"string"
            },
            "gear": {"items":"array","cash":"number"}
          }
        }]
        ```
        displayName and description can be in the manual's language.
        Include ALL attributes, skills, derived stats, identity fields, gear from the rulebook.
        """;

    /// <summary>
    /// Semantic search queries targeting each major entity category present in RPG manuals.
    /// Running one query per category ensures broad coverage.
    /// </summary>
    private static readonly IReadOnlyList<string> EntitySearchQueries =
    [
        "player character creation stats attributes abilities skills life path resolve stress",
        "npc non-player character enemies allies contacts",
        "vehicle car bike aircraft stats speed armor starship spacecraft ship hull weapons shields",
        "monster creature beast alien stats combat"
    ];

    private readonly IDocumentRepository _documentRepository;
    private readonly IEmbeddingService _embeddingService;
    private readonly IRagGenerationService _ragGeneration;
    private readonly ILogger<EntityExtractionRagService> _logger;

    /// <summary>
    /// Initialises the service with its dependencies.
    /// </summary>
    public EntityExtractionRagService(
        IDocumentRepository documentRepository,
        IEmbeddingService embeddingService,
        IRagGenerationService ragGeneration,
        ILogger<EntityExtractionRagService> logger)
    {
        _documentRepository = documentRepository;
        _embeddingService = embeddingService;
        _ragGeneration = ragGeneration;
        _logger = logger;
    }

    /// <inheritdoc/>
    public async Task<(IReadOnlyList<string> ContextChunks, Guid? FirstDocumentId)> GetExtractionContextAsync(
        Guid gameSystemId,
        Guid userId,
        bool isAdminOrOwner,
        int maxContextItems = 10,
        CancellationToken cancellationToken = default)
    {
        // Deduplicate results across all search queries by document ID.
        var allResults = new Dictionary<Guid, DocumentSearchResult>();
        Guid? firstDocumentId = null;

        foreach (var query in EntitySearchQueries)
        {
            var queryEmbedding = await _embeddingService.GetEmbeddingAsync(query, cancellationToken);

            var searchResults = await _documentRepository.SemanticSearchAsync(
                queryEmbedding,
                userId,
                limit: 8,
                threshold: 0.4f,
                gameSystemId: gameSystemId,
                skipOwnerFilter: isAdminOrOwner,
                cancellationToken: cancellationToken);

            foreach (var result in searchResults)
            {
                if (!allResults.ContainsKey(result.Document.Id))
                {
                    allResults[result.Document.Id] = result;
                    firstDocumentId ??= result.Document.Id;
                }
            }
        }

        if (allResults.Count == 0)
        {
            _logger.LogWarning(
                "No documents found for game system {GameSystemId}. Upload manuals first.",
                gameSystemId);
            return (Array.Empty<string>(), null);
        }

        _logger.LogInformation(
            "Found {ChunkCount} unique document chunks for entity extraction (skipOwnerFilter={SkipOwner})",
            allResults.Count, isAdminOrOwner);

        // Return the top N chunks ordered by similarity score.
        var contextChunks = allResults.Values
            .OrderByDescending(r => r.SimilarityScore)
            .Take(maxContextItems)
            .Select(r => r.Document.Content)
            .ToList();

        return (contextChunks, firstDocumentId);
    }

    /// <inheritdoc/>
    public async Task<string> GenerateEntityTypesAsync(
        IReadOnlyList<string> contextChunks,
        CancellationToken cancellationToken = default)
    {
        var ragResult = await _ragGeneration.GenerateWithContextAsync(
            ExtractionPrompt,
            contextChunks.ToList(),
            SystemPrompt,
            temperature: 0.3f,
            maxTokens: 8192,
            cancellationToken: cancellationToken);

        return ragResult.Answer;
    }
}
