using System.Text.Json;
using Loremaster.Domain.Common;
using Loremaster.Domain.Enums;

namespace Loremaster.Domain.Entities;

/// <summary>
/// AI/RAG generation request for traceability
/// </summary>
public class GenerationRequest : AuditableEntity
{
    public Guid UserId { get; private set; }
    public User User { get; private set; } = null!;

    public Guid? CampaignId { get; private set; }
    public Campaign? Campaign { get; private set; }

    public GenerationRequestType RequestType { get; private set; }
    public string TargetEntityType { get; private set; } = null!;
    public GenerationStatus Status { get; private set; } = GenerationStatus.Pending;
    
    public string? InputPrompt { get; private set; }
    
    /// <summary>
    /// Structured input (dice configs, table refs, OCR hints)
    /// Stored as JSONB - structure varies by request type
    /// </summary>
    public JsonDocument? InputParameters { get; private set; }
    
    public string? ErrorMessage { get; private set; }
    public DateTime? ProcessingStartedAt { get; private set; }
    public DateTime? ProcessingCompletedAt { get; private set; }

    // Navigation properties
    private readonly List<GenerationResult> _results = new();
    public IReadOnlyCollection<GenerationResult> Results => _results.AsReadOnly();

    private readonly List<LoreEntity> _generatedEntities = new();
    public IReadOnlyCollection<LoreEntity> GeneratedEntities => _generatedEntities.AsReadOnly();

    private GenerationRequest() { } // EF Core

    public static GenerationRequest Create(
        Guid userId,
        GenerationRequestType requestType,
        string targetEntityType,
        Guid? campaignId = null,
        string? inputPrompt = null,
        JsonDocument? inputParameters = null)
    {
        if (string.IsNullOrWhiteSpace(targetEntityType))
            throw new ArgumentException("Target entity type cannot be empty", nameof(targetEntityType));

        return new GenerationRequest
        {
            UserId = userId,
            CampaignId = campaignId,
            RequestType = requestType,
            TargetEntityType = targetEntityType.ToLowerInvariant().Trim(),
            InputPrompt = inputPrompt?.Trim(),
            InputParameters = inputParameters
        };
    }

    public void StartProcessing()
    {
        if (Status != GenerationStatus.Pending)
            throw new InvalidOperationException("Can only start processing from Pending status");

        Status = GenerationStatus.Processing;
        ProcessingStartedAt = DateTime.UtcNow;
    }

    public void Complete()
    {
        if (Status != GenerationStatus.Processing)
            throw new InvalidOperationException("Can only complete from Processing status");

        Status = GenerationStatus.Completed;
        ProcessingCompletedAt = DateTime.UtcNow;
    }

    public void Fail(string errorMessage)
    {
        Status = GenerationStatus.Failed;
        ErrorMessage = errorMessage;
        ProcessingCompletedAt = DateTime.UtcNow;
    }

    public TimeSpan? ProcessingDuration =>
        ProcessingStartedAt.HasValue && ProcessingCompletedAt.HasValue
            ? ProcessingCompletedAt.Value - ProcessingStartedAt.Value
            : null;
}
