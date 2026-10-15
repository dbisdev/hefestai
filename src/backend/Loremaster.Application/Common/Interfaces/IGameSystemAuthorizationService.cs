using Loremaster.Domain.Entities;

namespace Loremaster.Application.Common.Interfaces;

/// <summary>
/// Encapsulates the repeated "must be Admin or GameSystem owner" authorization check
/// that was previously duplicated across multiple command handlers.
/// </summary>
public interface IGameSystemAuthorizationService
{
    /// <summary>
    /// Verifies that <paramref name="userId"/> is allowed to perform <paramref name="action"/>
    /// on the given game system. Admins always pass; non-Admins must be the system owner.
    /// </summary>
    /// <param name="gameSystemId">ID of the game system to check ownership against.</param>
    /// <param name="userId">The ID of the requesting user.</param>
    /// <param name="isAdmin">Whether the user holds the Admin role.</param>
    /// <param name="action">Human-readable action name used in the exception message (e.g. "update templates").</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>
    /// The <see cref="GameSystem"/> entity after the check passes.
    /// Callers can use this to avoid a second repository lookup.
    /// </returns>
    /// <exception cref="Loremaster.Domain.Exceptions.NotFoundException">Thrown when the game system does not exist.</exception>
    /// <exception cref="Loremaster.Domain.Exceptions.ForbiddenAccessException">Thrown when the user is neither Admin nor system owner.</exception>
    Task<GameSystem> RequireOwnerOrAdminAsync(
        Guid gameSystemId,
        Guid userId,
        bool isAdmin,
        string action,
        CancellationToken cancellationToken = default);
}
