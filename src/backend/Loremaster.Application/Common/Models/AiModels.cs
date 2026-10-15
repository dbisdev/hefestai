namespace Loremaster.Application.Common.Models;

// ---------------------------------------------------------------------------
// Value objects
// ---------------------------------------------------------------------------

/// <summary>
/// Token usage statistics returned by AI generation operations.
/// </summary>
public record TokenUsage(
    int PromptTokens,
    int CompletionTokens,
    int TotalTokens);

// ---------------------------------------------------------------------------
// Result types
// ---------------------------------------------------------------------------

/// <summary>Result of a plain-text generation request.</summary>
public record GenerateTextResult(
    string Text,
    TokenUsage? Usage = null);

/// <summary>Result of a JSON-format generation request.</summary>
public record GenerateJsonResult(
    string Json,
    TokenUsage? Usage = null);

/// <summary>Result of an image generation request.</summary>
public record GenerateImageResult(
    string? ImageBase64,
    string? ImageUrl,
    bool Success);

/// <summary>Result of a text-summarization request.</summary>
public record SummarizeResult(
    string Summary,
    int OriginalLength,
    int SummaryLength,
    double CompressionRatio,
    TokenUsage? Usage = null);

/// <summary>Result of a chat generation request.</summary>
public record ChatResult(
    string Message,
    TokenUsage? Usage = null);

/// <summary>Result of a RAG generation request (<see cref="Interfaces.IRagGenerationService"/>).</summary>
public record RagGenerateResult(
    string Answer,
    TokenUsage? Usage = null);

// ---------------------------------------------------------------------------
// Input types
// ---------------------------------------------------------------------------

/// <summary>A single message in a multi-turn chat conversation.</summary>
public record ChatMessage(
    ChatRole Role,
    string Content);

// ---------------------------------------------------------------------------
// Enums
// ---------------------------------------------------------------------------

/// <summary>Role of a participant in a chat conversation.</summary>
public enum ChatRole
{
    User,
    Assistant,
    System
}

/// <summary>Style options for text summarization.</summary>
public enum SummarizeStyle
{
    Concise,
    Detailed,
    BulletPoints
}
