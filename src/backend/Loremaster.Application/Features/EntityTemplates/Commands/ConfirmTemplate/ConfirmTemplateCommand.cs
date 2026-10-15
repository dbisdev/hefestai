using MediatR;

namespace Loremaster.Application.Features.EntityTemplates.Commands.ConfirmTemplate;

/// <summary>
/// Command to confirm an entity template, making it available for entity creation.
/// </summary>
/// <param name="TemplateId">The template ID to confirm.</param>
/// <param name="GameSystemId">The game system ID for authorization.</param>
/// <param name="OwnerId">The user ID requesting the confirmation.</param>
/// <param name="Notes">Optional review notes.</param>
/// <param name="IsAdmin">If true, allows confirming any template regardless of ownership.</param>
public record ConfirmTemplateCommand(
    Guid TemplateId,
    Guid GameSystemId,
    Guid OwnerId,
    string? Notes = null,
    bool IsAdmin = false) : IRequest<ConfirmTemplateResult>;

/// <summary>
/// Result of template confirmation.
/// </summary>
public record ConfirmTemplateResult(
    Guid TemplateId,
    string EntityTypeName,
    DateTime ConfirmedAt);
