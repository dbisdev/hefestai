using Loremaster.Application.Common.Interfaces;
using Loremaster.Application.Common.Models;
using Microsoft.Extensions.Logging;

namespace Loremaster.Infrastructure.Services;

/// <summary>
/// HTTP client for communicating with Genkit AI microservice.
/// Handles text generation, image generation, summarization and chat.
/// Inherits shared HTTP transport and auth logic from <see cref="GenkitClientBase"/>.
/// Image compression is delegated to <see cref="IImageCompressionService"/>.
/// </summary>
public class GenkitAiService : GenkitClientBase, IAiService
{
    private readonly IImageCompressionService _imageCompression;

    /// <summary>Initializes a new instance of <see cref="GenkitAiService"/>.</summary>
    public GenkitAiService(
        HttpClient httpClient,
        IServiceTokenGenerator tokenGenerator,
        ILogger<GenkitAiService> logger,
        IImageCompressionService imageCompression)
        : base(httpClient, tokenGenerator, logger)
    {
        _imageCompression = imageCompression;
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
        var request = new GenerateRequest
        {
            Prompt = prompt,
            SystemPrompt = systemPrompt ?? "You are a helpful assistant. Always respond with valid minified JSON only, no additional text or markdown.",
            Temperature = temperature,
            MaxTokens = maxTokens,
            ResponseFormat = "json"  // Explicitly request JSON format for validation
        };

        var response = await SendRequestAsync<GenerateRequest, GenerateResponse>(
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

            var imageBase64 = response.Image?.Base64;
            
            // Compress image to WebP at 80% quality with max 500px width
            if (!string.IsNullOrEmpty(imageBase64))
            {
                imageBase64 = await _imageCompression.CompressToWebPAsync(imageBase64, 80, 500);
            }
            
            const string mimeType = "image/webp";
            
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
            Logger.LogWarning(ex, "Image generation failed");
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
            var response = await HttpClient.GetAsync("/health", cancellationToken);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "Genkit health check failed");
            return false;
        }
    }

    // Internal DTOs for Genkit API

    /// <summary>
    /// Request DTO for the <c>/api/generate</c> endpoint.
    /// Set <see cref="ResponseFormat"/> to <c>"json"</c> to enable JSON mode.
    /// </summary>
    private record GenerateRequest
    {
        public string Prompt { get; init; } = null!;
        public string? SystemPrompt { get; init; }
        public float Temperature { get; init; }
        public int MaxTokens { get; init; }
        /// <summary>Optional response format hint; pass <c>"json"</c> to request structured output.</summary>
        public string? ResponseFormat { get; init; }
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
}
