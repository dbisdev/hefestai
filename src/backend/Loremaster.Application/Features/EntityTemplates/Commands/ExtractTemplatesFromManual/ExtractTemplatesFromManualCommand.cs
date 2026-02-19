using Loremaster.Domain.ValueObjects;
using MediatR;

namespace Loremaster.Application.Features.EntityTemplates.Commands.ExtractTemplatesFromManual;

/// <summary>
/// Command to extract entity templates from a game system's manuals using RAG.
/// Analyzes uploaded manuals to detect entity types and their field schemas.
/// </summary>
/// <param name="GameSystemId">The game system to extract templates for.</param>
/// <param name="OwnerId">The owner ID for the templates (system owner).</param>
/// <param name="CurrentUserId">The current user ID making the request (for document search).</param>
/// <param name="SourceDocumentId">Optional specific document to analyze (null = all manuals).</param>
/// <param name="IsAdmin">Whether the requesting user is an admin (allows searching all owners' documents).</param>
/// <param name="IsSystemOwner">Whether the requesting user owns the game system (allows searching all documents in the system).</param>
public record ExtractTemplatesFromManualCommand(
    Guid GameSystemId,
    Guid OwnerId,
    Guid CurrentUserId,
    Guid? SourceDocumentId = null,
    bool IsAdmin = false,
    bool IsSystemOwner = false) : IRequest<ExtractTemplatesResult>;

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
/// When a confirmed template already exists, ExtractedFields contains the newly extracted fields
/// so the user can compare them with the existing template.
/// </summary>
/// <param name="TemplateId">ID of the template (existing if skipped, new if created).</param>
/// <param name="EntityTypeName">The entity type name (e.g., "player_character").</param>
/// <param name="DisplayName">Human-readable display name.</param>
/// <param name="FieldCount">Number of fields in the template.</param>
/// <param name="IsNew">Whether this is a newly created template.</param>
/// <param name="ExtractionNotes">Notes about the extraction (e.g., why it was skipped).</param>
/// <param name="ExtractedFields">For skipped templates: the newly extracted fields for comparison.</param>
public record ExtractedTemplateInfo(
    Guid TemplateId,
    string EntityTypeName,
    string DisplayName,
    int FieldCount,
    bool IsNew,
    string? ExtractionNotes,
    IReadOnlyList<FieldDefinition>? ExtractedFields = null);

