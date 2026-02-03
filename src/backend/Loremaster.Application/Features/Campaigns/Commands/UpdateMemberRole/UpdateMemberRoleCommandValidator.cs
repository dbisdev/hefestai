using FluentValidation;
using Loremaster.Domain.Enums;

namespace Loremaster.Application.Features.Campaigns.Commands.UpdateMemberRole;

/// <summary>
/// Validator for UpdateMemberRoleCommand ensuring required fields are valid.
/// </summary>
public class UpdateMemberRoleCommandValidator : AbstractValidator<UpdateMemberRoleCommand>
{
    public UpdateMemberRoleCommandValidator()
    {
        RuleFor(x => x.CampaignId)
            .NotEmpty().WithMessage("Campaign ID is required");

        RuleFor(x => x.MemberId)
            .NotEmpty().WithMessage("Member ID is required");

        RuleFor(x => x.NewRole)
            .IsInEnum().WithMessage("Invalid role specified");
    }
}
