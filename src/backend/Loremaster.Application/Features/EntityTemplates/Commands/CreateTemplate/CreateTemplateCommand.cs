using Loremaster.Application.Features.EntityTemplates.DTOs;
using MediatR;

namespace Loremaster.Application.Features.EntityTemplates.Commands.CreateTemplate;

/// <summary>
/// Command to manually create a new entity template.
/// Creates a template in Draft status that can be edited and later confirmed.
/// </summary>
/// <param name="GameSystemId">The game system this template belongs to.</param>
/// <param name="OwnerId">The owner (Master) creating this template.</param>
/// <param name="EntityTypeName">The normalized type name (e.g., "character", "vehicle").</param>
/// <param name="DisplayName">Human-readable display name.</param>
/// <param name="Description">Optional description of the entity type.</param>
/// <param name="IconHint">Optional icon or category hint for UI display.</param>
/// <param name="Version">Optional version identifier.</param>
/// <param name="Fields">Optional initial field definitions.</param>
public record CreateTemplateCommand(
    Guid GameSystemId,
    Guid OwnerId,
    string EntityTypeName,
    string DisplayName,
    string? Description = null,
    string? IconHint = null,
    string? Version = null,
    IReadOnlyList<FieldDefinitionDto>? Fields = null) : IRequest<CreateTemplateResult>;

/// <summary>
/// Result of template creation.
/// </summary>
/// <param name="TemplateId">The newly created template ID.</param>
/// <param name="EntityTypeName">The normalized entity type name.</param>
/// <param name="DisplayName">The display name.</param>
/// <param name="FieldCount">Number of field definitions.</param>
/// <param name="CreatedAt">When the template was created.</param>
public record CreateTemplateResult(
    Guid TemplateId,
    string EntityTypeName,
    string DisplayName,
    int FieldCount,
    DateTime CreatedAt);
