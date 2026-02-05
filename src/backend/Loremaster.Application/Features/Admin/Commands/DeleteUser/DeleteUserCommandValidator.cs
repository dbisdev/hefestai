using FluentValidation;

namespace Loremaster.Application.Features.Admin.Commands.DeleteUser;

/// <summary>
/// Validator for DeleteUserCommand.
/// </summary>
public class DeleteUserCommandValidator : AbstractValidator<DeleteUserCommand>
{
    public DeleteUserCommandValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty().WithMessage("User ID is required");
    }
}
