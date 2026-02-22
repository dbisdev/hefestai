using MediatR;

namespace Loremaster.Application.Features.EntityTemplates.Commands.RevertTemplateToDraft;

/// <summary>
/// Command to revert a confirmed template back to draft status.
/// Only the game system owner or an admin can perform this action.
/// </summary>
/// <param name="TemplateId">The template ID to revert.</param>
/// <param name="GameSystemId">The game system ID for authorization.</param>
/// <param name="OwnerId">The user ID requesting the revert.</param>
/// <param name="IsAdmin">If true, allows reverting any template.</param>
public record RevertTemplateToDraftCommand(
    Guid TemplateId,
    Guid GameSystemId,
    Guid OwnerId,
    bool IsAdmin = false) : IRequest<Unit>;
