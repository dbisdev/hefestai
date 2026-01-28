using FluentValidation;

namespace Loremaster.Application.Features.Projects.Commands.UpdateProject;

public class UpdateProjectCommandValidator : AbstractValidator<UpdateProjectCommand>
{
    public UpdateProjectCommandValidator()
    {
        RuleFor(x => x.ProjectId)
            .NotEmpty()
            .WithMessage("Project ID is required");

        RuleFor(x => x.Name)
            .NotEmpty()
            .WithMessage("Project name is required")
            .MaximumLength(200)
            .WithMessage("Project name cannot exceed 200 characters")
            .Matches(@"^[\w\s\-\.]+$")
            .WithMessage("Project name can only contain letters, numbers, spaces, hyphens, and dots");

        RuleFor(x => x.Description)
            .MaximumLength(2000)
            .WithMessage("Description cannot exceed 2000 characters")
            .When(x => !string.IsNullOrEmpty(x.Description));
    }
}
