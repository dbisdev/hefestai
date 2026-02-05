using Loremaster.Domain.Entities;

namespace Loremaster.Application.Common.Interfaces;

/// <summary>
/// Repository interface for Document entity with semantic search capabilities
/// </summary>
public interface IDocumentRepository
{
    Task<Document?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    
    Task<IReadOnlyList<Document>> GetByOwnerIdAsync(Guid ownerId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Semantic search using pgvector cosine distance
    /// </summary>
    /// <param name="queryEmbedding">The embedding vector for the search query.</param>
    /// <param name="ownerId">The owner ID to filter documents.</param>
    /// <param name="limit">Maximum number of results to return.</param>
    /// <param name="threshold">Minimum similarity threshold (0.0 to 1.0).</param>
    /// <param name="gameSystemId">Optional game system ID to filter documents (for RAG on manuals).</param>
    /// <param name="skipOwnerFilter">When true, skips owner filtering (for admin operations).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of documents with similarity scores.</returns>
    Task<IReadOnlyList<DocumentSearchResult>> SemanticSearchAsync(
        float[] queryEmbedding,
        Guid ownerId,
        int limit = 5,
        float threshold = 0.7f,
        Guid? gameSystemId = null,
        bool skipOwnerFilter = false,
        CancellationToken cancellationToken = default);
    
    Task<Document> AddAsync(Document document, CancellationToken cancellationToken = default);
    
    Task UpdateAsync(Document document, CancellationToken cancellationToken = default);
    
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
    
    Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Get documents that need embedding generation
    /// </summary>
    Task<IReadOnlyList<Document>> GetDocumentsWithoutEmbeddingAsync(
        Guid ownerId,
        int limit = 10,
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Get a parent document (manual) with its chunk count.
    /// </summary>
    /// <param name="manualId">The parent document ID.</param>
    /// <param name="ownerId">The owner ID for authorization.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The manual with chunk count, or null if not found.</returns>
    Task<ManualWithChunkCount?> GetManualWithChunkCountAsync(
        Guid manualId,
        Guid ownerId,
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Get all parent documents (manuals) for a game system with chunk counts.
    /// </summary>
    /// <param name="gameSystemId">The game system ID.</param>
    /// <param name="ownerId">The owner ID for authorization.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of manuals with chunk counts.</returns>
    Task<IReadOnlyList<ManualWithChunkCount>> GetManualsByGameSystemIdAsync(
        Guid gameSystemId,
        Guid ownerId,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Result of semantic search including similarity score
/// </summary>
public record DocumentSearchResult(
    Document Document,
    float SimilarityScore);

/// <summary>
/// Result containing a manual (parent document) with its chunk count.
/// </summary>
public record ManualWithChunkCount(
    Document Manual,
    int ChunkCount);
