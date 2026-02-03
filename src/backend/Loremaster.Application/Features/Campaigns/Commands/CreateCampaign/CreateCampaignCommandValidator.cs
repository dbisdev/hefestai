using FluentValidation;

namespace Loremaster.Application.Features.Campaigns.Commands.CreateCampaign;

/// <summary>
/// Validator for CreateCampaignCommand ensuring required fields are valid.
/// </summary>
public class CreateCampaignCommandValidator : AbstractValidator<CreateCampaignCommand>
{
    public CreateCampaignCommandValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Campaign name is required")
            .MaximumLength(100).WithMessage("Campaign name cannot exceed 100 characters");

        RuleFor(x => x.GameSystemId)
            .NotEmpty().WithMessage("Game system is required");

        RuleFor(x => x.Description)
            .MaximumLength(2000).WithMessage("Description cannot exceed 2000 characters")
            .When(x => x.Description != null);
    }
}
