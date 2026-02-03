using FluentValidation;

namespace Loremaster.Application.Features.Campaigns.Commands.JoinCampaign;

/// <summary>
/// Validator for JoinCampaignCommand ensuring the join code is provided.
/// </summary>
public class JoinCampaignCommandValidator : AbstractValidator<JoinCampaignCommand>
{
    public JoinCampaignCommandValidator()
    {
        RuleFor(x => x.JoinCode)
            .NotEmpty().WithMessage("Join code is required")
            .Length(8).WithMessage("Join code must be exactly 8 characters");
    }
}
