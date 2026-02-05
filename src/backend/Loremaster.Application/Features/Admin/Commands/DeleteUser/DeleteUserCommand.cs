using MediatR;

namespace Loremaster.Application.Features.Admin.Commands.DeleteUser;

/// <summary>
/// Command to delete a user (Admin only).
/// Performs a soft delete.
/// </summary>
/// <param name="UserId">The user ID to delete.</param>
public record DeleteUserCommand(Guid UserId) : IRequest<Unit>;
