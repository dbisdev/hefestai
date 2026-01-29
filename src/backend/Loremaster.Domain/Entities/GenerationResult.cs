using System.Text.Json;
using Loremaster.Domain.Common;

namespace Loremaster.Domain.Entities;

/// <summary>
/// AI/RAG generation output
/// </summary>
public class GenerationResult : BaseEntity
{
    public Guid GenerationRequestId { get; private set; }
    public GenerationRequest GenerationRequest { get; private set; } = null!;

    public string ResultType { get; private set; } = null!;
    public int SequenceOrder { get; private set; } = 1;
    
    /// <summary>
    /// Raw AI/RAG output
    /// Stored as JSONB - AI models return structured JSON (tool calls, reasoning chains)
    /// </summary>
    public JsonDocument? RawOutput { get; private set; }

    /// <summary>
    /// Parsed/normalized output
    /// Stored as JSONB - format depends on AI model and target entity type
    /// </summary>
    public JsonDocument? StructuredOutput { get; private set; }

    public string? ModelName { get; private set; }

    /// <summary>
    /// AI configuration (temperature, max_tokens, etc.)
    /// Stored as JSONB - varies by model provider
    /// </summary>
    public JsonDocument? ModelParameters { get; private set; }

    /// <summary>
    /// Token usage metrics
    /// Stored as JSONB - format varies by provider (OpenAI vs Google vs Anthropic)
    /// </summary>
    public JsonDocument? TokenUsage { get; private set; }

    public decimal? ConfidenceScore { get; private set; }

    // Navigation properties
    private readonly List<GenerationResultSource> _sources = new();
    public IReadOnlyCollection<GenerationResultSource> Sources => _sources.AsReadOnly();

    private GenerationResult() { } // EF Core

    public static GenerationResult Create(
        Guid generationRequestId,
        string resultType,
        int sequenceOrder = 1,
        JsonDocument? rawOutput = null,
        JsonDocument? structuredOutput = null,
        string? modelName = null,
        JsonDocument? modelParameters = null,
        JsonDocument? tokenUsage = null,
        decimal? confidenceScore = null)
    {
        if (string.IsNullOrWhiteSpace(resultType))
            throw new ArgumentException("Result type cannot be empty", nameof(resultType));

        if (confidenceScore.HasValue && (confidenceScore < 0 || confidenceScore > 1))
            throw new ArgumentOutOfRangeException(nameof(confidenceScore), "Confidence score must be between 0 and 1");

        return new GenerationResult
        {
            GenerationRequestId = generationRequestId,
            ResultType = resultType.ToLowerInvariant().Trim(),
            SequenceOrder = sequenceOrder,
            RawOutput = rawOutput,
            StructuredOutput = structuredOutput,
            ModelName = modelName?.Trim(),
            ModelParameters = modelParameters,
            TokenUsage = tokenUsage,
            ConfidenceScore = confidenceScore
        };
    }
}
