namespace Loremaster.Application.Common.Interfaces;

/// <summary>
/// Represents a chunk of text with metadata about its position.
/// </summary>
public record TextChunk
{
    /// <summary>
    /// The text content of this chunk.
    /// </summary>
    public required string Content { get; init; }
    
    /// <summary>
    /// Zero-based index of this chunk in the original document.
    /// </summary>
    public required int Index { get; init; }
    
    /// <summary>
    /// Character offset where this chunk starts in the original text.
    /// </summary>
    public required int StartOffset { get; init; }
    
    /// <summary>
    /// Character offset where this chunk ends in the original text.
    /// </summary>
    public required int EndOffset { get; init; }
    
    /// <summary>
    /// Length of the chunk in characters.
    /// </summary>
    public int Length => Content.Length;
}

/// <summary>
/// Configuration options for text chunking.
/// </summary>
public record ChunkingOptions
{
    /// <summary>
    /// Maximum number of characters per chunk.
    /// Default is 1000 characters (roughly 200-250 tokens).
    /// </summary>
    public int MaxChunkSize { get; init; } = 1000;
    
    /// <summary>
    /// Number of characters to overlap between consecutive chunks.
    /// Helps maintain context across chunk boundaries.
    /// Default is 200 characters.
    /// </summary>
    public int OverlapSize { get; init; } = 200;
    
    /// <summary>
    /// Minimum chunk size to create. Chunks smaller than this will be merged with the previous chunk.
    /// Default is 100 characters.
    /// </summary>
    public int MinChunkSize { get; init; } = 100;
    
    /// <summary>
    /// Whether to preserve paragraph boundaries when possible.
    /// Default is true.
    /// </summary>
    public bool PreserveParagraphs { get; init; } = true;
    
    /// <summary>
    /// Whether to preserve sentence boundaries when possible.
    /// Default is true.
    /// </summary>
    public bool PreserveSentences { get; init; } = true;

    /// <summary>
    /// Default chunking options suitable for most RAG use cases.
    /// </summary>
    public static ChunkingOptions Default => new();
    
    /// <summary>
    /// Options optimized for larger context windows.
    /// </summary>
    public static ChunkingOptions LargeChunks => new()
    {
        MaxChunkSize = 2000,
        OverlapSize = 400,
        MinChunkSize = 200
    };
    
    /// <summary>
    /// Options optimized for smaller, more precise chunks.
    /// </summary>
    public static ChunkingOptions SmallChunks => new()
    {
        MaxChunkSize = 500,
        OverlapSize = 100,
        MinChunkSize = 50
    };
}

/// <summary>
/// Service for splitting text into chunks suitable for embedding and RAG.
/// </summary>
public interface ITextChunkingService
{
    /// <summary>
    /// Splits text into chunks using the specified options.
    /// </summary>
    /// <param name="text">The text to split.</param>
    /// <param name="options">Chunking configuration options.</param>
    /// <returns>List of text chunks with metadata.</returns>
    IReadOnlyList<TextChunk> ChunkText(string text, ChunkingOptions? options = null);
    
    /// <summary>
    /// Splits text into chunks using default options.
    /// </summary>
    /// <param name="text">The text to split.</param>
    /// <returns>List of text chunks with metadata.</returns>
    IReadOnlyList<TextChunk> ChunkText(string text);
}
