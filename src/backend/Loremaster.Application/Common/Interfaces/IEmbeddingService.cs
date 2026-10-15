using Loremaster.Application.Common.Models;

namespace Loremaster.Application.Common.Interfaces;

/// <summary>
/// Interface for embedding-vector operations via Genkit microservice.
/// RAG generation (using those vectors) is handled by <see cref="IRagGenerationService"/>.
/// </summary>
public interface IEmbeddingService
{
    /// <summary>
    /// Generate embeddings for a list of texts.
    /// </summary>
    Task<EmbeddingsResult> GetEmbeddingsAsync(
        IEnumerable<string> texts,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Generate an embedding vector for a single text.
    /// </summary>
    Task<float[]> GetEmbeddingAsync(
        string text,
        CancellationToken cancellationToken = default);
}

// ── Embedding DTOs (embedding-specific; not shared with AI service) ─────────

/// <summary>Batch of embedding vectors produced by the embedding service.</summary>
public record EmbeddingsResult(
    float[][] Embeddings,
    string Model,
    int Dimensions);
