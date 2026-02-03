using FluentValidation;

namespace Loremaster.Application.Features.GameSystems.Commands.CreateGameSystem;

/// <summary>
/// Validator for CreateGameSystemCommand ensuring required fields are valid.
/// </summary>
public class CreateGameSystemCommandValidator : AbstractValidator<CreateGameSystemCommand>
{
    public CreateGameSystemCommandValidator()
    {
        RuleFor(x => x.Code)
            .NotEmpty().WithMessage("Code is required")
            .MaximumLength(50).WithMessage("Code cannot exceed 50 characters")
            .Matches("^[a-z0-9-]+$").WithMessage("Code must contain only lowercase letters, numbers, and hyphens");

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
