namespace Loremaster.Application.Common.Interfaces;

/// <summary>
/// Service interface for retrieving RAG context for entity generation.
/// Retrieves relevant document chunks from game system manuals.
/// </summary>
public interface IRagContextProvider
{
    /// <summary>
    /// Retrieves relevant context chunks for entity generation.
    /// Searches game system manuals using semantic search.
    /// </summary>
    /// <param name="gameSystemId">The game system to scope the search.</param>
    /// <param name="ownerId">The owner ID for authorization.</param>
    /// <param name="entityTypeName">The entity type being generated (e.g., "character").</param>
    /// <param name="additionalContext">Optional additional search context (user prompt).</param>
    /// <param name="maxChunks">Maximum number of chunks to return.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of relevant context chunks from manuals.</returns>
    Task<IReadOnlyList<RagContextChunk>> GetContextForEntityGenerationAsync(
        Guid gameSystemId,
        Guid ownerId,
        string entityTypeName,
        string? additionalContext = null,
        int maxChunks = 5,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves context specifically for image prompt generation.
    /// Focuses on visual descriptions and style from manuals.
    /// </summary>
    /// <param name="gameSystemId">The game system to scope the search.</param>
    /// <param name="ownerId">The owner ID for authorization.</param>
    /// <param name="entityTypeName">The entity type being visualized.</param>
    /// <param name="additionalContext">Optional additional context (e.g., species, role, morphology) to refine style search.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of style and visual context chunks.</returns>
    Task<IReadOnlyList<RagContextChunk>> GetStyleContextAsync(
        Guid gameSystemId,
        Guid ownerId,
        string entityTypeName,
        string? additionalContext = null,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// A chunk of RAG context retrieved from game manuals.
/// </summary>
public record RagContextChunk(
    /// <summary>
    /// The text content of the chunk.
    /// </summary>
    string Content,
    
    /// <summary>
    /// The source document title.
    /// </summary>
    string SourceTitle,
    
    /// <summary>
    /// The source document ID.
    /// </summary>
    Guid SourceDocumentId,
    
    /// <summary>
    /// Similarity score from semantic search (0.0 to 1.0).
    /// </summary>
    float SimilarityScore);
