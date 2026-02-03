using Loremaster.Application.Features.EntityTemplates.DTOs;
using MediatR;

namespace Loremaster.Application.Features.EntityTemplates.Commands.UpdateTemplate;

/// <summary>
/// Command to update an entity template's metadata and field definitions.
/// Only allowed for templates in Draft or PendingReview status.
/// </summary>
/// <param name="TemplateId">The template ID to update.</param>
/// <param name="OwnerId">The owner ID for authorization.</param>
/// <param name="DisplayName">New display name.</param>
/// <param name="Description">New description.</param>
/// <param name="IconHint">New icon hint.</param>
/// <param name="Version">New version.</param>
/// <param name="Fields">Updated field definitions (if provided).</param>
public record UpdateTemplateCommand(
    Guid TemplateId,
    Guid OwnerId,
    string DisplayName,
    string? Description = null,
    string? IconHint = null,
    string? Version = null,
    IReadOnlyList<FieldDefinitionDto>? Fields = null) : IRequest<UpdateTemplateResult>;

/// <summary>
/// Result of template update.
/// </summary>
public record UpdateTemplateResult(
    Guid TemplateId,
    string EntityTypeName,
    string DisplayName,
    int FieldCount,
    DateTime UpdatedAt);
