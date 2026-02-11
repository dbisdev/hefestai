using Loremaster.Domain.Common;
using Loremaster.Domain.Enums;
using Pgvector;

namespace Loremaster.Domain.Entities;

/// <summary>
/// Document entity for RAG - stores text content with vector embeddings.
/// Can be associated with a GameSystem for game-specific RAG queries.
/// </summary>
public class Document : AuditableEntity
{
    public string Title { get; private set; } = null!;
    public string Content { get; private set; } = null!;
    public string? Source { get; private set; }
    public string? Metadata { get; private set; }
    
    /// <summary>
    /// Vector embedding for semantic search (pgvector type)
    /// </summary>
    public Vector? Embedding { get; private set; }
    
    /// <summary>
    /// Embedding dimensions (3072 for gemini-embedding-001)
    /// </summary>
    public int? EmbeddingDimensions { get; private set; }
    
    /// <summary>
    /// Owner of this document (Master user)
    /// </summary>
    public Guid OwnerId { get; private set; }
    public User Owner { get; private set; } = null!;
    
    /// <summary>
    /// Optional game system association for system-specific RAG queries.
    /// When set, this document will only be included in searches for this game system.
    /// </summary>
    public Guid? GameSystemId { get; private set; }
    public GameSystem? GameSystem { get; private set; }
    
    /// <summary>
    /// Type of RAG source (Rulebook, Supplement, Custom)
    /// </summary>
    public RagSourceType? SourceType { get; private set; }
    
    /// <summary>
    /// Chunk index if this document is part of a larger document.
    /// Null if this is a standalone document.
    /// </summary>
    public int? ChunkIndex { get; private set; }
    
    /// <summary>
    /// Parent document ID if this is a chunk.
    /// Used to link chunks back to their source document.
    /// </summary>
    public Guid? ParentDocumentId { get; private set; }

    private Document() { } // EF Core

    public static Document Create(
        string title,
        string content,
        Guid ownerId,
        string? source = null,
        string? metadata = null,
        Guid? gameSystemId = null,
        RagSourceType? sourceType = null)
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("Title cannot be empty", nameof(title));
        if (string.IsNullOrWhiteSpace(content))
            throw new ArgumentException("Content cannot be empty", nameof(content));

        return new Document
        {
            Title = title.Trim(),
            Content = content,
            OwnerId = ownerId,
            Source = source?.Trim(),
            Metadata = metadata,
            GameSystemId = gameSystemId,
            SourceType = sourceType
        };
    }

    /// <summary>
    /// Creates a document chunk from a larger document.
    /// </summary>
    public static Document CreateChunk(
        string title,
        string content,
        Guid ownerId,
        Guid parentDocumentId,
        int chunkIndex,
        string? source = null,
        Guid? gameSystemId = null,
        RagSourceType? sourceType = null)
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("Title cannot be empty", nameof(title));
        if (string.IsNullOrWhiteSpace(content))
            throw new ArgumentException("Content cannot be empty", nameof(content));
        if (chunkIndex < 0)
            throw new ArgumentException("Chunk index must be non-negative", nameof(chunkIndex));

        return new Document
        {
            Title = title.Trim(),
            Content = content,
            OwnerId = ownerId,
            Source = source?.Trim(),
            ParentDocumentId = parentDocumentId,
            ChunkIndex = chunkIndex,
            GameSystemId = gameSystemId,
            SourceType = sourceType
        };
    }

    public void UpdateContent(string title, string content, string? source = null, string? metadata = null)
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("Title cannot be empty", nameof(title));
        if (string.IsNullOrWhiteSpace(content))
            throw new ArgumentException("Content cannot be empty", nameof(content));

        Title = title.Trim();
        Content = content;
        Source = source?.Trim();
        Metadata = metadata;
        
        // Clear embedding when content changes - needs re-embedding
        Embedding = null;
        EmbeddingDimensions = null;
    }

    public void SetEmbedding(float[] embedding)
    {
        if (embedding == null)
            throw new ArgumentNullException(nameof(embedding));
        
        Embedding = new Vector(embedding);
        EmbeddingDimensions = embedding.Length;
    }

    public void ClearEmbedding()
    {
        Embedding = null;
        EmbeddingDimensions = null;
    }

    public bool HasEmbedding => Embedding != null && EmbeddingDimensions > 0;

    public bool IsOwnedBy(Guid userId) => OwnerId == userId;
    
    /// <summary>
    /// Checks if this document is a chunk of a larger document.
    /// </summary>
    public bool IsChunk => ParentDocumentId.HasValue && ChunkIndex.HasValue;
    
    /// <summary>
    /// Associates this document with a game system.
    /// </summary>
    public void SetGameSystem(Guid gameSystemId, RagSourceType? sourceType = null)
    {
        GameSystemId = gameSystemId;
        if (sourceType.HasValue)
            SourceType = sourceType.Value;
    }
    
    /// <summary>
    /// Removes the game system association.
    /// </summary>
    public void ClearGameSystem()
    {
        GameSystemId = null;
        SourceType = null;
    }
}
