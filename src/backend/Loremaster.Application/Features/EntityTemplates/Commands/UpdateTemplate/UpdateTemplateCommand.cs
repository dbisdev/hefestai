using Loremaster.Application.Features.EntityTemplates.DTOs;
using MediatR;

namespace Loremaster.Application.Features.EntityTemplates.Commands.UpdateTemplate;

/// <summary>
/// Command to update an entity template's metadata and field definitions.
/// Only allowed for templates in Draft or PendingReview status, unless IsAdmin is true.
/// </summary>
/// <param name="TemplateId">The template ID to update.</param>
/// <param name="GameSystemId">The game system ID for authorization.</param>
/// <param name="OwnerId">The owner ID for authorization.</param>
/// <param name="DisplayName">New display name.</param>
/// <param name="EntityTypeName">New entity type name (optional, e.g., "character", "actor").</param>
/// <param name="Description">New description.</param>
/// <param name="IconHint">New icon hint.</param>
/// <param name="Version">New version.</param>
/// <param name="Fields">Updated field definitions (if provided).</param>
/// <param name="IsAdmin">If true, allows updating confirmed templates (admin override).</param>
public record UpdateTemplateCommand(
    Guid TemplateId,
    Guid GameSystemId,
    Guid OwnerId,
    string DisplayName,
    string? EntityTypeName = null,
    string? Description = null,
    string? IconHint = null,
    string? Version = null,
    IReadOnlyList<FieldDefinitionDto>? Fields = null,
    bool IsAdmin = false) : IRequest<UpdateTemplateResult>;

/// <summary>
/// Result of template update.
/// </summary>
public record UpdateTemplateResult(
    Guid TemplateId,
    string EntityTypeName,
    string DisplayName,
    int FieldCount,
    DateTime UpdatedAt);
