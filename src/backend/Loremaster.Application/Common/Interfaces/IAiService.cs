namespace Loremaster.Application.Common.Interfaces;

/// <summary>
/// Interface for AI service operations via Genkit microservice
/// </summary>
public interface IAiService
{
    /// <summary>
    /// Generate text content from a prompt
    /// </summary>
    Task<GenerateTextResult> GenerateTextAsync(
        string prompt, 
        string? systemPrompt = null,
        float temperature = 0.7f,
        int maxTokens = 2048,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Generate structured JSON content from a prompt
    /// </summary>
    Task<GenerateJsonResult> GenerateJsonAsync(
        string prompt,
        string? systemPrompt = null,
        float temperature = 0.7f,
        int maxTokens = 2048,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Generate an image from a prompt
    /// </summary>
    Task<GenerateImageResult> GenerateImageAsync(
        string prompt,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Summarize text content
    /// </summary>
    Task<SummarizeResult> SummarizeAsync(
        string text,
        SummarizeStyle style = SummarizeStyle.Concise,
        int maxLength = 500,
        string language = "en",
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Chat with multi-turn conversation support
    /// </summary>
    Task<ChatResult> ChatAsync(
        IEnumerable<ChatMessage> messages,
        string? context = null,
        float temperature = 0.7f,
        int maxTokens = 2048,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Check if the AI service is healthy
    /// </summary>
    Task<bool> IsHealthyAsync(CancellationToken cancellationToken = default);
}

// DTOs for AI Service
public record GenerateTextResult(
    string Text,
    TokenUsage? Usage = null);

public record GenerateJsonResult(
    string Json,
    TokenUsage? Usage = null);

public record GenerateImageResult(
    string? ImageBase64,
    string? ImageUrl,
    bool Success);

public record SummarizeResult(
    string Summary,
    int OriginalLength,
    int SummaryLength,
    double CompressionRatio,
    TokenUsage? Usage = null);

public record ChatResult(
    string Message,
    TokenUsage? Usage = null);

public record ChatMessage(
    ChatRole Role,
    string Content);

public record TokenUsage(
    int PromptTokens,
    int CompletionTokens,
    int TotalTokens);

public enum ChatRole
{
    User,
    Assistant,
    System
}

public enum SummarizeStyle
{
    Concise,
    Detailed,
    BulletPoints
}
