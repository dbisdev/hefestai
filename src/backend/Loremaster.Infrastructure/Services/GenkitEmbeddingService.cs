using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Loremaster.Application.Common.Interfaces;
using Microsoft.Extensions.Logging;

namespace Loremaster.Infrastructure.Services;

/// <summary>
/// HTTP client for embedding operations via Genkit microservice
/// </summary>
public class GenkitEmbeddingService : IEmbeddingService
{
    private readonly HttpClient _httpClient;
    private readonly IServiceTokenGenerator _tokenGenerator;
    private readonly ILogger<GenkitEmbeddingService> _logger;
    private readonly JsonSerializerOptions _jsonOptions;

    public GenkitEmbeddingService(
        HttpClient httpClient,
        IServiceTokenGenerator tokenGenerator,
        ILogger<GenkitEmbeddingService> logger)
    {
        _httpClient = httpClient;
        _tokenGenerator = tokenGenerator;
        _logger = logger;

        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        };
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
            response.Usage != null 
                ? new TokenUsage(response.Usage.PromptTokens, response.Usage.CompletionTokens, response.Usage.TotalTokens)
                : null);
    }

    private async Task<TResponse> SendRequestAsync<TRequest, TResponse>(
        string endpoint,
        TRequest request,
        CancellationToken cancellationToken)
    {
        var token = _tokenGenerator.GenerateServiceToken(ServiceScopes.GenkitExecute);

        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, endpoint);
        httpRequest.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        httpRequest.Content = JsonContent.Create(request, options: _jsonOptions);

        _logger.LogDebug("Sending embedding request to Genkit: {Endpoint}", endpoint);

        var response = await _httpClient.SendAsync(httpRequest, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogError("Genkit embedding request failed: {StatusCode} - {Content}",
                response.StatusCode, errorContent);

            throw new HttpRequestException(
                $"Genkit request failed with status {response.StatusCode}: {errorContent}");
        }

        var result = await response.Content.ReadFromJsonAsync<TResponse>(_jsonOptions, cancellationToken);

        if (result == null)
        {
            throw new InvalidOperationException("Failed to deserialize Genkit response");
        }

        return result;
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

    private record TokenUsageDto
    {
        public int PromptTokens { get; init; }
        public int CompletionTokens { get; init; }
        public int TotalTokens { get; init; }
    }
}
