using Loremaster.Domain.Common;
using Pgvector;

namespace Loremaster.Domain.Entities;

/// <summary>
/// Document entity for RAG - stores text content with vector embeddings
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
    /// Embedding dimensions (typically 768 for text-embedding-004)
    /// </summary>
    public int? EmbeddingDimensions { get; private set; }
    
    /// <summary>
    /// Owner of this document (Master user)
    /// </summary>
    public Guid OwnerId { get; private set; }
    public User Owner { get; private set; } = null!;
    
    /// <summary>
    /// Optional project association
    /// </summary>
    public Guid? ProjectId { get; private set; }
    public Project? Project { get; private set; }

    private Document() { } // EF Core

    public static Document Create(
        string title,
        string content,
        Guid ownerId,
        string? source = null,
        string? metadata = null,
        Guid? projectId = null)
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
            ProjectId = projectId
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
}
