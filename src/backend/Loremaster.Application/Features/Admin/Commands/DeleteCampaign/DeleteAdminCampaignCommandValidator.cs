using FluentValidation;

namespace Loremaster.Application.Features.Admin.Commands.DeleteCampaign;

/// <summary>
/// Validator for DeleteAdminCampaignCommand.
/// </summary>
public class DeleteAdminCampaignCommandValidator : AbstractValidator<DeleteAdminCampaignCommand>
{
    public DeleteAdminCampaignCommandValidator()
    {
        RuleFor(x => x.CampaignId)
            .NotEmpty().WithMessage("Campaign ID is required");
    }
}
