using FluentValidation;
using Loremaster.Domain.Enums;

namespace Loremaster.Application.Features.LoreEntities.Commands.ChangeEntityVisibility;

/// <summary>
/// Validator for ChangeEntityVisibilityCommand ensuring required fields are valid.
/// Authorization checks (campaign membership, entity ownership) are performed in the handler.
/// </summary>
public class ChangeEntityVisibilityCommandValidator : AbstractValidator<ChangeEntityVisibilityCommand>
{
    public ChangeEntityVisibilityCommandValidator()
    {
        RuleFor(x => x.CampaignId)
            .NotEmpty().WithMessage("Campaign ID is required");

        RuleFor(x => x.EntityId)
            .NotEmpty().WithMessage("Entity ID is required");

        RuleFor(x => x.Visibility)
            .IsInEnum().WithMessage("Visibility must be a valid VisibilityLevel value (Draft=0, Private=1, Campaign=2, Public=3)");
    }
}
