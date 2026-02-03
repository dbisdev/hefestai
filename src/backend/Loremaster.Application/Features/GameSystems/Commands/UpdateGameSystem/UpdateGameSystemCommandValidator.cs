using FluentValidation;

namespace Loremaster.Application.Features.GameSystems.Commands.UpdateGameSystem;

/// <summary>
/// Validator for UpdateGameSystemCommand ensuring required fields are valid.
/// </summary>
public class UpdateGameSystemCommandValidator : AbstractValidator<UpdateGameSystemCommand>
{
    public UpdateGameSystemCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("Game system ID is required");

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Name is required")
            .MaximumLength(100).WithMessage("Name cannot exceed 100 characters");

        RuleFor(x => x.Publisher)
            .MaximumLength(100).WithMessage("Publisher cannot exceed 100 characters")
            .When(x => x.Publisher != null);

        RuleFor(x => x.Version)
            .MaximumLength(50).WithMessage("Version cannot exceed 50 characters")
            .When(x => x.Version != null);

        RuleFor(x => x.Description)
            .MaximumLength(2000).WithMessage("Description cannot exceed 2000 characters")
            .When(x => x.Description != null);
    }
}
