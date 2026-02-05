using Loremaster.Application.Common.Interfaces;
using Loremaster.Domain.Exceptions;
using MediatR;

namespace Loremaster.Application.Features.Admin.Commands.DeleteUser;

/// <summary>
/// Handler for DeleteUserCommand. Soft deletes a user.
/// </summary>
public class DeleteUserCommandHandler : IRequestHandler<DeleteUserCommand, Unit>
{
    private readonly IUserRepository _userRepository;
    private readonly IUnitOfWork _unitOfWork;

    public DeleteUserCommandHandler(
        IUserRepository userRepository,
        IUnitOfWork unitOfWork)
    {
        _userRepository = userRepository;
        _unitOfWork = unitOfWork;
    }

    /// <summary>
    /// Handles the delete user command.
    /// </summary>
    /// <param name="request">The command containing the user ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Unit.</returns>
    /// <exception cref="NotFoundException">Thrown when user not found.</exception>
    /// <exception cref="DomainException">Thrown when trying to delete the last admin.</exception>
    public async Task<Unit> Handle(
        DeleteUserCommand request,
        CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByIdAsync(request.UserId, cancellationToken)
            ?? throw new NotFoundException("User", request.UserId);

        // Prevent deleting the last admin user
        if (user.IsAdmin)
        {
            var admins = await _userRepository.GetByRoleAsync(Domain.Enums.UserRole.Admin, cancellationToken);
            var activeAdmins = admins.Where(a => a.IsActive && !a.IsDeleted).ToList();
            
            if (activeAdmins.Count <= 1)
            {
                throw new DomainException("Cannot delete the last admin user");
            }
        }

        // Soft delete
        _userRepository.Delete(user);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Unit.Value;
    }
}
