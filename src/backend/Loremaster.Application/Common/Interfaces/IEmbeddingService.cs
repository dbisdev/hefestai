namespace Loremaster.Application.Common.Interfaces;

/// <summary>
/// Interface for embedding service operations via Genkit microservice
/// </summary>
public interface IEmbeddingService
{
    /// <summary>
    /// Generate embeddings for a list of texts
    /// </summary>
    Task<EmbeddingsResult> GetEmbeddingsAsync(
        IEnumerable<string> texts,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Generate embedding for a single text
    /// </summary>
    Task<float[]> GetEmbeddingAsync(
        string text,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Generate answer using RAG with provided context
    /// </summary>
    Task<RagGenerateResult> GenerateWithContextAsync(
        string query,
        IEnumerable<string> context,
        string? systemPrompt = null,
        float temperature = 0.3f,
        int maxTokens = 2048,
        CancellationToken cancellationToken = default);
}

// DTOs for Embedding Service
public record EmbeddingsResult(
    float[][] Embeddings,
    string Model,
    int Dimensions);

public record RagGenerateResult(
    string Answer,
    TokenUsage? Usage = null);
