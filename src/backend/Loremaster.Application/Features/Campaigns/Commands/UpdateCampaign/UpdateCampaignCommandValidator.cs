using FluentValidation;

namespace Loremaster.Application.Features.Campaigns.Commands.UpdateCampaign;

/// <summary>
/// Validator for UpdateCampaignCommand ensuring required fields are valid.
/// </summary>
public class UpdateCampaignCommandValidator : AbstractValidator<UpdateCampaignCommand>
{
    public UpdateCampaignCommandValidator()
    {
        RuleFor(x => x.CampaignId)
            .NotEmpty().WithMessage("Campaign ID is required");

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Campaign name is required")
            .MaximumLength(100).WithMessage("Campaign name cannot exceed 100 characters");

        RuleFor(x => x.Description)
            .MaximumLength(2000).WithMessage("Description cannot exceed 2000 characters")
            .When(x => x.Description != null);
    }
}
