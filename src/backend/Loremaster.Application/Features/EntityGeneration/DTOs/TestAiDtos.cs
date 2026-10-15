namespace Loremaster.Application.Features.EntityGeneration.DTOs;

/// <summary>Request DTO for the AI test endpoint.</summary>
public record TestAiRequest
{
    /// <summary>Prompt to send to the AI service.</summary>
    public string Prompt { get; init; } = "Hello, can you confirm you are working?";

    /// <summary>Optional system prompt to configure AI behaviour.</summary>
    public string? SystemPrompt { get; init; }

    /// <summary>Sampling temperature (0.0–1.0). Defaults to 0.7 when null.</summary>
    public float? Temperature { get; init; }

    /// <summary>Maximum tokens to generate. Defaults to 256 when null.</summary>
    public int? MaxTokens { get; init; }
}

/// <summary>Response DTO for the AI test endpoint.</summary>
public record TestAiResponse
{
    /// <summary>Whether the AI call completed without errors.</summary>
    public bool Success { get; init; }

    /// <summary>Generated text, if successful.</summary>
    public string? Text { get; init; }

    /// <summary>Error message, if unsuccessful.</summary>
    public string? Error { get; init; }

    /// <summary>Token usage details, if returned by the AI service.</summary>
    public TokenUsageResponse? Usage { get; init; }
}

/// <summary>Token consumption details surfaced in the API response.</summary>
public record TokenUsageResponse
{
    /// <summary>Tokens consumed by the prompt.</summary>
    public int PromptTokens { get; init; }

    /// <summary>Tokens generated in the completion.</summary>
    public int CompletionTokens { get; init; }

    /// <summary>Total tokens consumed.</summary>
    public int TotalTokens { get; init; }
}
