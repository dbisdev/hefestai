using System.Text.Json;
using Loremaster.Domain.Common;
using Loremaster.Domain.Enums;

namespace Loremaster.Domain.Entities;

/// <summary>
/// PDF/OCR import history for lore entities
/// </summary>
public class LoreEntityImport : AuditableEntity
{
    public Guid LoreEntityId { get; private set; }
    public LoreEntity LoreEntity { get; private set; } = null!;

    public Guid UserId { get; private set; }
    public User User { get; private set; } = null!;

    public ImportType ImportType { get; private set; }
    public string SourceFilename { get; private set; } = null!;
    public string? SourceFileUrl { get; private set; }
    public string? FileHash { get; private set; }

    /// <summary>
    /// Raw OCR/PDF extraction output
    /// Stored as JSONB - structure depends on source document format
    /// </summary>
    public JsonDocument? ExtractionResult { get; private set; }

    /// <summary>
    /// Field-to-attribute mapping
    /// Stored as JSONB - user-defined variable mapping
    /// </summary>
    public JsonDocument? FieldMapping { get; private set; }

    public GenerationStatus ProcessingStatus { get; private set; } = GenerationStatus.Pending;
    public string? ErrorDetails { get; private set; }

    private LoreEntityImport() { } // EF Core

    public static LoreEntityImport Create(
        Guid loreEntityId,
        Guid userId,
        ImportType importType,
        string sourceFilename,
        string? sourceFileUrl = null,
        string? fileHash = null)
    {
        if (string.IsNullOrWhiteSpace(sourceFilename))
            throw new ArgumentException("Source filename cannot be empty", nameof(sourceFilename));

        return new LoreEntityImport
        {
            LoreEntityId = loreEntityId,
            UserId = userId,
            ImportType = importType,
            SourceFilename = sourceFilename.Trim(),
            SourceFileUrl = sourceFileUrl?.Trim(),
            FileHash = fileHash
        };
    }

    public void StartProcessing()
    {
        if (ProcessingStatus != GenerationStatus.Pending)
            throw new InvalidOperationException("Can only start processing from Pending status");

        ProcessingStatus = GenerationStatus.Processing;
    }

    public void Complete(JsonDocument? extractionResult = null, JsonDocument? fieldMapping = null)
    {
        if (ProcessingStatus != GenerationStatus.Processing)
            throw new InvalidOperationException("Can only complete from Processing status");

        ProcessingStatus = GenerationStatus.Completed;
        ExtractionResult = extractionResult;
        FieldMapping = fieldMapping;
    }

    public void Fail(string errorDetails)
    {
        ProcessingStatus = GenerationStatus.Failed;
        ErrorDetails = errorDetails;
    }

    public void UpdateFieldMapping(JsonDocument fieldMapping)
    {
        FieldMapping?.Dispose();
        FieldMapping = fieldMapping;
    }
}
