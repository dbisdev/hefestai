using Loremaster.Application.Common.Interfaces;
using Loremaster.Domain.Entities;
using Loremaster.Domain.Exceptions;

namespace Loremaster.Application.Common.Services;

/// <summary>
/// Centralizes the "must be Admin or GameSystem owner" authorization rule.
/// 
/// Before this service existed, every EntityTemplate command handler duplicated
/// the same 6-line block:
///   1. await _gameSystemRepository.GetByIdAsync(...)
///   2. throw ArgumentException if null
///   3. var isSystemOwner = gameSystem.OwnerId == userId
///   4. if (!isAdmin &amp;&amp; !isSystemOwner) throw UnauthorizedAccessException
///
/// This service collapses those four steps into a single awaitable call and returns
/// the resolved <see cref="GameSystem"/> so callers never need a second lookup.
/// </summary>
public sealed class GameSystemAuthorizationService : IGameSystemAuthorizationService
{
    private readonly IGameSystemRepository _gameSystemRepository;

    /// <summary>
    /// Initializes a new instance of <see cref="GameSystemAuthorizationService"/>.
    /// </summary>
    /// <param name="gameSystemRepository">Repository for game system lookups.</param>
    public GameSystemAuthorizationService(IGameSystemRepository gameSystemRepository)
    {
        _gameSystemRepository = gameSystemRepository;
    }

    /// <inheritdoc/>
    public async Task<GameSystem> RequireOwnerOrAdminAsync(
        Guid gameSystemId,
        Guid userId,
        bool isAdmin,
        string action,
        CancellationToken cancellationToken = default)
    {
        var gameSystem = await _gameSystemRepository.GetByIdAsync(gameSystemId, cancellationToken);

        if (gameSystem is null)
        {
            throw new NotFoundException(nameof(GameSystem), gameSystemId);
        }

        var isSystemOwner = gameSystem.OwnerId == userId;
        if (!isAdmin && !isSystemOwner)
        {
            throw new ForbiddenAccessException(
                $"You do not have permission to {action} in this game system. " +
                $"Only the system owner or an Admin can perform this action.");
        }

        return gameSystem;
    }
}
