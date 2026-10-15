using Loremaster.Domain.Entities;

namespace Loremaster.Application.Common.Interfaces;

public interface IGameSystemRepository
{
    Task<GameSystem?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<GameSystem?> GetByCodeAsync(string code, CancellationToken cancellationToken = default);
    Task<IEnumerable<GameSystem>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<IEnumerable<GameSystem>> GetActiveAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns active game systems visible to the given caller.
    /// - <paramref name="includeAll"/> = true (Admin): all active systems.
    /// - <paramref name="isPlayer"/> = true (Player): Admin-owned systems + systems owned by
    ///   Masters in any campaign the Player belongs to.
    /// - Otherwise (Master): Admin-owned systems + own systems.
    /// - Fallback (no userId): only Admin-owned systems.
    /// </summary>
    Task<IEnumerable<GameSystem>> GetActiveForUserAsync(
        Guid? userId,
        bool includeAll,
        bool isPlayer = false,
        CancellationToken cancellationToken = default);
    Task<bool> ExistsByCodeAsync(string code, CancellationToken cancellationToken = default);
    Task<bool> ExistsByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task AddAsync(GameSystem gameSystem, CancellationToken cancellationToken = default);
    void Update(GameSystem gameSystem);
    void Delete(GameSystem gameSystem);
}
