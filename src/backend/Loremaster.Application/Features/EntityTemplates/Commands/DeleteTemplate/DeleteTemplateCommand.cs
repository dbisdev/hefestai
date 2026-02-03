using MediatR;

namespace Loremaster.Application.Features.EntityTemplates.Commands.DeleteTemplate;

/// <summary>
/// Command to delete an entity template.
/// Templates can only be deleted if they are not being used by any entities.
/// </summary>
/// <param name="TemplateId">The template ID to delete.</param>
/// <param name="OwnerId">The owner ID for authorization.</param>
/// <param name="ForceDelete">If true, deletes even if entities exist using this template (they become orphaned).</param>
public record DeleteTemplateCommand(
    Guid TemplateId,
    Guid OwnerId,
    bool ForceDelete = false) : IRequest<DeleteTemplateResult>;

/// <summary>
/// Result of template deletion.
/// </summary>
/// <param name="TemplateId">The deleted template ID.</param>
/// <param name="EntityTypeName">The template's entity type name.</param>
/// <param name="WasForced">Whether the deletion was forced despite existing entities.</param>
/// <param name="AffectedEntityCount">Number of entities that were using this template (if forced).</param>
public record DeleteTemplateResult(
    Guid TemplateId,
    string EntityTypeName,
    bool WasForced,
    int AffectedEntityCount);
