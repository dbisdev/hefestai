using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Loremaster.Application.Common.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Webp;
using SixLabors.ImageSharp.Processing;

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
            SystemPrompt = systemPrompt ?? "You are a helpful assistant. Always respond with valid minified JSON only, no additional text or markdown.",
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

            var imageBase64 = response.Image?.Base64;
            
            // Compress image to WebP at 80% quality with max 500px width
            if (!string.IsNullOrEmpty(imageBase64))
            {
                imageBase64 = await CompressImageToWebPAsync(imageBase64, 80, 500);
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
            _logger.LogWarning(ex, "Image generation failed");
            return new GenerateImageResult(null, null, false);
        }
    }

    /// <summary>
    /// Compresses an image to WebP format with specified quality and max width.
    /// </summary>
    /// <param name="base64Image">Base64 encoded image data</param>
    /// <param name="quality">Compression quality (0-100)</param>
    /// <param name="maxWidth">Maximum width in pixels</param>
    /// <returns>Base64 encoded WebP image</returns>
    private async Task<string> CompressImageToWebPAsync(string base64Image, int quality = 80, int maxWidth = 500)
    {
        try
        {
            // Remove data URL prefix if present
            var imageData = base64Image.Contains(',') 
                ? base64Image.Split(',')[1] 
                : base64Image;
            
            var imageBytes = Convert.FromBase64String(imageData);
            var originalSize = imageBytes.Length;
            
            _logger.LogInformation("Compressing image: {OriginalSize} bytes to WebP", originalSize);
            
            using var inputStream = new MemoryStream(imageBytes);
            using var outputStream = new MemoryStream();
            
            using (var image = await Image.LoadAsync(inputStream))
            {
                _logger.LogInformation("Image loaded: {Width}x{Height}", image.Width, image.Height);
                
                // Resize if wider than maxWidth while maintaining aspect ratio
                if (image.Width > maxWidth)
                {
                    var newHeight = (int)((double)image.Height * maxWidth / image.Width);
                    _logger.LogInformation("Resizing to {Width}x{Height}", maxWidth, newHeight);
                    image.Mutate(x => x.Resize(maxWidth, newHeight));
                }
                
                // Encode to WebP with specified quality
                var encoder = new WebpEncoder
                {
                    Quality = quality,
                    FileFormat = WebpFileFormatType.Lossy
                };
                
                await image.SaveAsync(outputStream, encoder);
            }
            
            var result = Convert.ToBase64String(outputStream.ToArray());
            var compressedSize = result.Length;
            var savings = ((double)(originalSize - compressedSize) / originalSize * 100).ToString("F1");
            
            _logger.LogInformation("Image compressed: {OriginalSize} -> {CompressedSize} bytes ({Savings}% smaller)", 
                originalSize, compressedSize, savings);
            
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Image compression failed. Original size: {Size}", base64Image.Length);
            throw; // Re-throw to let the caller handle the failure
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
