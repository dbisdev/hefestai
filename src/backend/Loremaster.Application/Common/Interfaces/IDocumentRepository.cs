using Loremaster.Domain.Entities;

namespace Loremaster.Application.Common.Interfaces;

/// <summary>
/// Repository interface for Document entity with semantic search capabilities
/// </summary>
public interface IDocumentRepository
{
    Task<Document?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    
    Task<IReadOnlyList<Document>> GetByOwnerIdAsync(Guid ownerId, CancellationToken cancellationToken = default);
    
    Task<IReadOnlyList<Document>> GetByProjectIdAsync(Guid projectId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Semantic search using pgvector cosine distance
    /// </summary>
    Task<IReadOnlyList<DocumentSearchResult>> SemanticSearchAsync(
        float[] queryEmbedding,
        Guid ownerId,
        int limit = 5,
        float threshold = 0.7f,
        Guid? projectId = null,
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
}

/// <summary>
/// Result of semantic search including similarity score
/// </summary>
public record DocumentSearchResult(
    Document Document,
    float SimilarityScore);
