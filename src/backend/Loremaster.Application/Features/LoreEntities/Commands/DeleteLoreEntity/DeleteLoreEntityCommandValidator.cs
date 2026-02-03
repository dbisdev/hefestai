using FluentValidation;

namespace Loremaster.Application.Features.LoreEntities.Commands.DeleteLoreEntity;

/// <summary>
/// Validator for DeleteLoreEntityCommand ensuring required fields are valid.
/// Authorization checks (campaign membership, entity ownership) are performed in the handler.
/// </summary>
public class DeleteLoreEntityCommandValidator : AbstractValidator<DeleteLoreEntityCommand>
{
    public DeleteLoreEntityCommandValidator()
    {
        RuleFor(x => x.CampaignId)
            .NotEmpty().WithMessage("Campaign ID is required");

        RuleFor(x => x.EntityId)
            .NotEmpty().WithMessage("Entity ID is required");
    }
}
