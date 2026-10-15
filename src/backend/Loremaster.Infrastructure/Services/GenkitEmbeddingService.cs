using Loremaster.Application.Common.Interfaces;
using Loremaster.Application.Common.Models;
using Microsoft.Extensions.Logging;

namespace Loremaster.Infrastructure.Services;

/// <summary>
/// HTTP client for embedding and RAG generation operations via Genkit microservice.
/// Implements both <see cref="IEmbeddingService"/> (vector embeddings) and
/// <see cref="IRagGenerationService"/> (context-grounded answer generation) because
/// both operations share the same HTTP transport and the Genkit endpoint set.
/// Inherits shared HTTP transport and auth logic from <see cref="GenkitClientBase"/>.
/// </summary>
public class GenkitEmbeddingService : GenkitClientBase, IEmbeddingService, IRagGenerationService
{
    public GenkitEmbeddingService(
        HttpClient httpClient,
        IServiceTokenGenerator tokenGenerator,
        ILogger<GenkitEmbeddingService> logger)
        : base(httpClient, tokenGenerator, logger)
    {
    }

    public async Task<EmbeddingsResult> GetEmbeddingsAsync(
        IEnumerable<string> texts,
        CancellationToken cancellationToken = default)
    {
        var textList = texts.ToList();
        if (textList.Count == 0)
            throw new ArgumentException("At least one text is required", nameof(texts));

        var request = new EmbeddingsRequest
        {
            Texts = textList
        };

        var response = await SendRequestAsync<EmbeddingsRequest, EmbeddingsResponse>(
            "/api/embeddings", request, cancellationToken);

        return new EmbeddingsResult(
            response.Embeddings,
            response.Model,
            response.Dimensions);
    }

    public async Task<float[]> GetEmbeddingAsync(
        string text,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(text))
            throw new ArgumentException("Text cannot be empty", nameof(text));

        var result = await GetEmbeddingsAsync(new[] { text }, cancellationToken);
        Logger.LogInformation(
            "GetEmbeddingAsync returned embedding with {Dimensions} dimensions from model {Model}",
            result.Embeddings[0].Length, result.Model);
        return result.Embeddings[0];
    }

    public async Task<RagGenerateResult> GenerateWithContextAsync(
        string query,
        IEnumerable<string> context,
        string? systemPrompt = null,
        float temperature = 0.3f,
        int maxTokens = 2048,
        CancellationToken cancellationToken = default)
    {
        var contextList = context.ToList();
        if (contextList.Count == 0)
            throw new ArgumentException("At least one context document is required", nameof(context));

        var request = new RagGenerateRequest
        {
            Query = query,
            Context = contextList,
            SystemPrompt = systemPrompt,
            Temperature = temperature,
            MaxTokens = maxTokens
        };

        var response = await SendRequestAsync<RagGenerateRequest, RagGenerateResponse>(
            "/api/rag/generate", request, cancellationToken);

        return new RagGenerateResult(
            response.Answer,
            MapTokenUsage(response.Usage));
    }

    // Internal DTOs for Genkit Embedding API
    private record EmbeddingsRequest
    {
        public List<string> Texts { get; init; } = new();
        public string? Model { get; init; }
    }

    private record EmbeddingsResponse
    {
        public float[][] Embeddings { get; init; } = Array.Empty<float[]>();
        public string Model { get; init; } = null!;
        public int Dimensions { get; init; }
    }

    private record RagGenerateRequest
    {
        public string Query { get; init; } = null!;
        public List<string> Context { get; init; } = new();
        public string? SystemPrompt { get; init; }
        public float Temperature { get; init; }
        public int MaxTokens { get; init; }
    }

    private record RagGenerateResponse
    {
        public string Answer { get; init; } = null!;
        public TokenUsageDto? Usage { get; init; }
    }
}
