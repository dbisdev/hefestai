using MediatR;

namespace Loremaster.Application.Features.EntityTemplates.Commands.ConfirmTemplate;

/// <summary>
/// Command to confirm an entity template, making it available for entity creation.
/// </summary>
/// <param name="TemplateId">The template ID to confirm.</param>
/// <param name="OwnerId">The owner ID for authorization.</param>
/// <param name="Notes">Optional review notes.</param>
public record ConfirmTemplateCommand(
    Guid TemplateId,
    Guid OwnerId,
    string? Notes = null) : IRequest<ConfirmTemplateResult>;

/// <summary>
/// Result of template confirmation.
/// </summary>
public record ConfirmTemplateResult(
    Guid TemplateId,
    string EntityTypeName,
    DateTime ConfirmedAt);
