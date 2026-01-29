using Loremaster.Domain.Entities;

namespace Loremaster.Application.Common.Interfaces;

public interface IGameSystemRepository
{
    Task<GameSystem?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<GameSystem?> GetByCodeAsync(string code, CancellationToken cancellationToken = default);
    Task<IEnumerable<GameSystem>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<IEnumerable<GameSystem>> GetActiveAsync(CancellationToken cancellationToken = default);
    Task<bool> ExistsByCodeAsync(string code, CancellationToken cancellationToken = default);
    Task AddAsync(GameSystem gameSystem, CancellationToken cancellationToken = default);
    void Update(GameSystem gameSystem);
    void Delete(GameSystem gameSystem);
}
