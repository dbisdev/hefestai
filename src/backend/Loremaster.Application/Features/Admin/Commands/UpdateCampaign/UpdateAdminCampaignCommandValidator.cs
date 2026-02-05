using FluentValidation;

namespace Loremaster.Application.Features.Admin.Commands.UpdateCampaign;

/// <summary>
/// Validator for UpdateAdminCampaignCommand.
/// </summary>
public class UpdateAdminCampaignCommandValidator : AbstractValidator<UpdateAdminCampaignCommand>
{
    public UpdateAdminCampaignCommandValidator()
    {
        RuleFor(x => x.CampaignId)
            .NotEmpty().WithMessage("Campaign ID is required");

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Campaign name cannot be empty when provided")
            .MaximumLength(200).WithMessage("Campaign name must not exceed 200 characters")
            .When(x => x.Name != null);

        RuleFor(x => x.Description)
            .MaximumLength(2000).WithMessage("Description must not exceed 2000 characters")
            .When(x => x.Description != null);
    }
}
