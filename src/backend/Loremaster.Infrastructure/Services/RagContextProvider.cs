using Loremaster.Application.Common.Interfaces;
using Microsoft.Extensions.Logging;

namespace Loremaster.Infrastructure.Services;

/// <summary>
/// Provides RAG context for entity generation by querying game system manuals.
/// Uses semantic search to find relevant document chunks.
/// </summary>
public class RagContextProvider : IRagContextProvider
{
    private readonly IDocumentRepository _documentRepository;
    private readonly IEmbeddingService _embeddingService;
    private readonly ILogger<RagContextProvider> _logger;

    /// <summary>
    /// Default similarity threshold for semantic search.
    /// </summary>
    private const float DefaultSimilarityThreshold = 0.6f;

    public RagContextProvider(
        IDocumentRepository documentRepository,
        IEmbeddingService embeddingService,
        ILogger<RagContextProvider> logger)
    {
        _documentRepository = documentRepository ?? throw new ArgumentNullException(nameof(documentRepository));
        _embeddingService = embeddingService ?? throw new ArgumentNullException(nameof(embeddingService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<RagContextChunk>> GetContextForEntityGenerationAsync(
        Guid gameSystemId,
        Guid ownerId,
        string entityTypeName,
        string? additionalContext = null,
        int maxChunks = 5,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug(
            "Getting RAG context for entity generation. GameSystem: {GameSystemId}, EntityType: {EntityType}, MaxChunks: {MaxChunks}",
            gameSystemId, entityTypeName, maxChunks);

        try
        {
            // Build the search query combining entity type and any additional context
            var searchQuery = BuildEntityGenerationQuery(entityTypeName, additionalContext);

            // Get embedding for the search query
            var queryEmbedding = await _embeddingService.GetEmbeddingAsync(searchQuery, cancellationToken);

            // Perform semantic search scoped to the game system
            var searchResults = await _documentRepository.SemanticSearchAsync(
                queryEmbedding,
                ownerId,
                limit: maxChunks,
                threshold: DefaultSimilarityThreshold,
                gameSystemId: gameSystemId,
                cancellationToken: cancellationToken);

            if (!searchResults.Any())
            {
                _logger.LogWarning(
                    "No RAG context found for entity type '{EntityType}' in game system {GameSystemId}",
                    entityTypeName, gameSystemId);
                return Array.Empty<RagContextChunk>();
            }

            var chunks = searchResults
                .Select(result => new RagContextChunk(
                    Content: result.Document.Content,
                    SourceTitle: result.Document.Title,
                    SourceDocumentId: result.Document.Id,
                    SimilarityScore: result.SimilarityScore))
                .ToList();

            _logger.LogDebug(
                "Found {ChunkCount} RAG context chunks for entity type '{EntityType}'",
                chunks.Count, entityTypeName);

            return chunks;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Failed to get RAG context for entity type '{EntityType}' in game system {GameSystemId}",
                entityTypeName, gameSystemId);
            
            // Return empty list rather than throwing - generation can proceed without RAG context
            return Array.Empty<RagContextChunk>();
        }
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<RagContextChunk>> GetStyleContextAsync(
        Guid gameSystemId,
        Guid ownerId,
        string entityTypeName,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug(
            "Getting style context for image generation. GameSystem: {GameSystemId}, EntityType: {EntityType}",
            gameSystemId, entityTypeName);

        try
        {
            // Build a query focused on visual/style descriptions
            var styleQuery = BuildStyleQuery(entityTypeName);

            // Get embedding for the style query
            var queryEmbedding = await _embeddingService.GetEmbeddingAsync(styleQuery, cancellationToken);

            // Search with a lower threshold since style context may be less specific
            var searchResults = await _documentRepository.SemanticSearchAsync(
                queryEmbedding,
                ownerId,
                limit: 3, // Fewer chunks needed for style context
                threshold: 0.5f, // Lower threshold for broader style matches
                gameSystemId: gameSystemId,
                cancellationToken: cancellationToken);

            var chunks = searchResults
                .Select(result => new RagContextChunk(
                    Content: result.Document.Content,
                    SourceTitle: result.Document.Title,
                    SourceDocumentId: result.Document.Id,
                    SimilarityScore: result.SimilarityScore))
                .ToList();

            _logger.LogDebug(
                "Found {ChunkCount} style context chunks for entity type '{EntityType}'",
                chunks.Count, entityTypeName);

            return chunks;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Failed to get style context for entity type '{EntityType}' in game system {GameSystemId}",
                entityTypeName, gameSystemId);
            
            // Return empty list - image generation can proceed without style context
            return Array.Empty<RagContextChunk>();
        }
    }

    /// <summary>
    /// Builds a search query for entity generation context.
    /// </summary>
    /// <param name="entityTypeName">The entity type being generated.</param>
    /// <param name="additionalContext">Optional additional context from user prompt.</param>
    /// <returns>Search query string.</returns>
    private static string BuildEntityGenerationQuery(string entityTypeName, string? additionalContext)
    {
        // Start with entity type
        var queryParts = new List<string>
        {
            entityTypeName,
            $"{entityTypeName} creation",
            $"{entityTypeName} attributes",
            $"{entityTypeName} characteristics"
        };

        // Add additional context if provided
        if (!string.IsNullOrWhiteSpace(additionalContext))
        {
            queryParts.Add(additionalContext);
        }

        // Combine into a comprehensive search query
        return string.Join(" ", queryParts);
    }

    /// <summary>
    /// Builds a search query for visual style context.
    /// </summary>
    /// <param name="entityTypeName">The entity type being visualized.</param>
    /// <returns>Style-focused search query.</returns>
    private static string BuildStyleQuery(string entityTypeName)
    {
        return $"{entityTypeName} appearance visual description art style artwork illustration artist graphic designer ambience history culture";
    }
}
