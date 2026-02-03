using MediatR;

namespace Loremaster.Application.Features.EntityTemplates.Commands.ExtractTemplatesFromManual;

/// <summary>
/// Command to extract entity templates from a game system's manuals using RAG.
/// Analyzes uploaded manuals to detect entity types and their field schemas.
/// </summary>
/// <param name="GameSystemId">The game system to extract templates for.</param>
/// <param name="OwnerId">The owner (Master) requesting extraction.</param>
/// <param name="SourceDocumentId">Optional specific document to analyze (null = all manuals).</param>
public record ExtractTemplatesFromManualCommand(
    Guid GameSystemId,
    Guid OwnerId,
    Guid? SourceDocumentId = null) : IRequest<ExtractTemplatesResult>;

/// <summary>
/// Result of template extraction from manuals.
/// </summary>
public record ExtractTemplatesResult(
    int TemplatesCreated,
    int TemplatesUpdated,
    int TemplatesSkipped,
    IReadOnlyList<ExtractedTemplateInfo> Templates,
    string? ErrorMessage = null);

/// <summary>
/// Information about a single extracted template.
/// </summary>
public record ExtractedTemplateInfo(
    Guid TemplateId,
    string EntityTypeName,
    string DisplayName,
    int FieldCount,
    bool IsNew,
    string? ExtractionNotes);
