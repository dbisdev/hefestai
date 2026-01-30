using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Loremaster.Application.Common.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Loremaster.Infrastructure.Services;

/// <summary>
/// HTTP client for communicating with Genkit AI microservice
/// </summary>
public class GenkitAiService : IAiService
{
    private readonly HttpClient _httpClient;
    private readonly IServiceTokenGenerator _tokenGenerator;
    private readonly ILogger<GenkitAiService> _logger;
    private readonly JsonSerializerOptions _jsonOptions;

    public GenkitAiService(
        HttpClient httpClient,
        IServiceTokenGenerator tokenGenerator,
        ILogger<GenkitAiService> logger)
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

    public async Task<GenerateTextResult> GenerateTextAsync(
        string prompt,
        string? systemPrompt = null,
        float temperature = 0.7f,
        int maxTokens = 2048,
        CancellationToken cancellationToken = default)
    {
        var request = new GenerateRequest
        {
            Prompt = prompt,
            SystemPrompt = systemPrompt,
            Temperature = temperature,
            MaxTokens = maxTokens
        };

        var response = await SendRequestAsync<GenerateRequest, GenerateResponse>(
            "/api/generate", request, cancellationToken);

        return new GenerateTextResult(
            response.Text,
            MapTokenUsage(response.Usage));
    }

    public async Task<GenerateJsonResult> GenerateJsonAsync(
        string prompt,
        string? systemPrompt = null,
        float temperature = 0.7f,
        int maxTokens = 2048,
        CancellationToken cancellationToken = default)
    {
        var request = new GenerateJsonRequest
        {
            Prompt = prompt,
            SystemPrompt = systemPrompt ?? "You are a helpful assistant. Always respond with valid JSON only, no additional text.",
            Temperature = temperature,
            MaxTokens = maxTokens,
            ResponseFormat = "json"
        };

        var response = await SendRequestAsync<GenerateJsonRequest, GenerateResponse>(
            "/api/generate", request, cancellationToken);

        return new GenerateJsonResult(
            response.Text,
            MapTokenUsage(response.Usage));
    }

    public async Task<GenerateImageResult> GenerateImageAsync(
        string prompt,
        string? style = null,
        string aspectRatio = "1:1",
        string? negativePrompt = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var request = new ImageGenerateRequest
            {
                Prompt = prompt,
                Style = style,
                AspectRatio = aspectRatio,
                NegativePrompt = negativePrompt
            };

            var response = await SendRequestAsync<ImageGenerateRequest, ImageGenerateResponse>(
                "/api/generate-image", request, cancellationToken);

            // Convert new response format to existing result format
            var imageBase64 = response.Image?.Base64;
            var mimeType = response.Image?.MimeType ?? "image/png";
            
            // Create data URL if we have base64 data
            string? imageUrl = imageBase64 != null 
                ? $"data:{mimeType};base64,{imageBase64}" 
                : null;

            return new GenerateImageResult(
                imageBase64,
                imageUrl,
                response.Success);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Image generation failed");
            return new GenerateImageResult(null, null, false);
        }
    }

    public async Task<SummarizeResult> SummarizeAsync(
        string text,
        SummarizeStyle style = SummarizeStyle.Concise,
        int maxLength = 500,
        string language = "en",
        CancellationToken cancellationToken = default)
    {
        var styleString = style switch
        {
            SummarizeStyle.Concise => "concise",
            SummarizeStyle.Detailed => "detailed",
            SummarizeStyle.BulletPoints => "bullet-points",
            _ => "concise"
        };

        var request = new SummarizeRequest
        {
            Text = text,
            Style = styleString,
            MaxLength = maxLength,
            Language = language
        };

        var response = await SendRequestAsync<SummarizeRequest, SummarizeResponse>(
            "/api/summarize", request, cancellationToken);

        return new SummarizeResult(
            response.Summary,
            response.OriginalLength,
            response.SummaryLength,
            response.CompressionRatio,
            MapTokenUsage(response.Usage));
    }

    public async Task<ChatResult> ChatAsync(
        IEnumerable<ChatMessage> messages,
        string? context = null,
        float temperature = 0.7f,
        int maxTokens = 2048,
        CancellationToken cancellationToken = default)
    {
        var request = new ChatRequest
        {
            Messages = messages.Select(m => new ChatMessageDto
            {
                Role = m.Role.ToString().ToLowerInvariant(),
                Content = m.Content
            }).ToList(),
            Context = context,
            Temperature = temperature,
            MaxTokens = maxTokens
        };

        var response = await SendRequestAsync<ChatRequest, ChatResponse>(
            "/api/chat", request, cancellationToken);

        return new ChatResult(
            response.Message,
            MapTokenUsage(response.Usage));
    }

    public async Task<bool> IsHealthyAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.GetAsync("/health", cancellationToken);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Genkit health check failed");
            return false;
        }
    }

    private async Task<TResponse> SendRequestAsync<TRequest, TResponse>(
        string endpoint,
        TRequest request,
        CancellationToken cancellationToken)
    {
        // Generate service token
        var token = _tokenGenerator.GenerateServiceToken(ServiceScopes.GenkitExecute);

        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, endpoint);
        httpRequest.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        httpRequest.Content = JsonContent.Create(request, options: _jsonOptions);

        _logger.LogDebug("Sending request to Genkit: {Endpoint}", endpoint);

        var response = await _httpClient.SendAsync(httpRequest, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogError("Genkit request failed: {StatusCode} - {Content}", 
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

    private static TokenUsage? MapTokenUsage(TokenUsageDto? usage)
    {
        if (usage == null) return null;
        return new TokenUsage(usage.PromptTokens, usage.CompletionTokens, usage.TotalTokens);
    }

    // Internal DTOs for Genkit API
    private record GenerateRequest
    {
        public string Prompt { get; init; } = null!;
        public string? SystemPrompt { get; init; }
        public float Temperature { get; init; }
        public int MaxTokens { get; init; }
    }

    private record GenerateJsonRequest
    {
        public string Prompt { get; init; } = null!;
        public string? SystemPrompt { get; init; }
        public float Temperature { get; init; }
        public int MaxTokens { get; init; }
        public string ResponseFormat { get; init; } = "json";
    }

    private record GenerateResponse
    {
        public string Text { get; init; } = null!;
        public TokenUsageDto? Usage { get; init; }
    }

    private record ImageGenerateRequest
    {
        public string Prompt { get; init; } = null!;
        public string? NegativePrompt { get; init; }
        public string AspectRatio { get; init; } = "1:1";
        public string? Style { get; init; }
    }

    private record ImageGenerateResponse
    {
        public ImageDataDto? Image { get; init; }
        public bool Success { get; init; }
        public string? Message { get; init; }
        public string? UsedPrompt { get; init; }
    }

    private record ImageDataDto
    {
        public string Base64 { get; init; } = null!;
        public string MimeType { get; init; } = null!;
    }

    private record SummarizeRequest
    {
        public string Text { get; init; } = null!;
        public string Style { get; init; } = "concise";
        public int MaxLength { get; init; }
        public string Language { get; init; } = "en";
    }

    private record SummarizeResponse
    {
        public string Summary { get; init; } = null!;
        public int OriginalLength { get; init; }
        public int SummaryLength { get; init; }
        public double CompressionRatio { get; init; }
        public TokenUsageDto? Usage { get; init; }
    }

    private record ChatRequest
    {
        public List<ChatMessageDto> Messages { get; init; } = new();
        public string? Context { get; init; }
        public float Temperature { get; init; }
        public int MaxTokens { get; init; }
    }

    private record ChatMessageDto
    {
        public string Role { get; init; } = null!;
        public string Content { get; init; } = null!;
    }

    private record ChatResponse
    {
        public string Message { get; init; } = null!;
        public TokenUsageDto? Usage { get; init; }
    }

    private record TokenUsageDto
    {
        public int PromptTokens { get; init; }
        public int CompletionTokens { get; init; }
        public int TotalTokens { get; init; }
    }
}
