using Loremaster.Domain.Entities;
using Loremaster.Domain.Enums;

namespace Loremaster.Application.Common.Interfaces;

public interface IWorldEntityRepository
{
    Task<WorldEntity?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IEnumerable<WorldEntity>> GetByCreatorIdAsync(Guid creatorId, CancellationToken cancellationToken = default);
    Task<IEnumerable<WorldEntity>> GetByCreatorIdAndCategoryAsync(Guid creatorId, EntityCategory category, CancellationToken cancellationToken = default);
    Task<IEnumerable<WorldEntity>> GetVisibleEntitiesAsync(Guid userId, Guid? masterId, CancellationToken cancellationToken = default);
    Task AddAsync(WorldEntity entity, CancellationToken cancellationToken = default);
    void Update(WorldEntity entity);
    void Delete(WorldEntity entity);
}
