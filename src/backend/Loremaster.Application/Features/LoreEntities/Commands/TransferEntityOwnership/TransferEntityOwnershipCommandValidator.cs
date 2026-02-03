using FluentValidation;

namespace Loremaster.Application.Features.LoreEntities.Commands.TransferEntityOwnership;

/// <summary>
/// Validator for TransferEntityOwnershipCommand ensuring required fields are valid.
/// </summary>
public class TransferEntityOwnershipCommandValidator : AbstractValidator<TransferEntityOwnershipCommand>
{
    public TransferEntityOwnershipCommandValidator()
    {
        RuleFor(x => x.CampaignId)
            .NotEmpty().WithMessage("Campaign ID is required");

        RuleFor(x => x.EntityId)
            .NotEmpty().WithMessage("Entity ID is required");

        RuleFor(x => x.NewOwnerId)
            .NotEmpty().WithMessage("New owner ID is required");

        RuleFor(x => x.NewOwnerId)
            .NotEqual(x => Guid.Empty).WithMessage("New owner ID must be a valid GUID");
    }
}
