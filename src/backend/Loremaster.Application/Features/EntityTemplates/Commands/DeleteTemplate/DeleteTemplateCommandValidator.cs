using FluentValidation;

namespace Loremaster.Application.Features.EntityTemplates.Commands.DeleteTemplate;

/// <summary>
/// Validator for DeleteTemplateCommand.
/// </summary>
public class DeleteTemplateCommandValidator : AbstractValidator<DeleteTemplateCommand>
{
    public DeleteTemplateCommandValidator()
    {
        RuleFor(x => x.TemplateId)
            .NotEmpty()
            .WithMessage("Template ID is required");

        RuleFor(x => x.OwnerId)
            .NotEmpty()
            .WithMessage("Owner ID is required");
    }
}
