using Loremaster.Domain.Entities;
using Loremaster.Domain.Enums;

namespace Loremaster.Application.Common.Interfaces;

public interface IRagSourceRepository
{
    Task<RagSource?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IEnumerable<RagSource>> GetByGameSystemIdAsync(Guid gameSystemId, CancellationToken cancellationToken = default);
    Task<IEnumerable<RagSource>> GetActiveByGameSystemIdAsync(Guid gameSystemId, CancellationToken cancellationToken = default);
    Task<IEnumerable<RagSource>> GetByTypeAsync(RagSourceType sourceType, CancellationToken cancellationToken = default);
    Task<RagSource?> GetByContentHashAsync(string contentHash, CancellationToken cancellationToken = default);
    Task AddAsync(RagSource ragSource, CancellationToken cancellationToken = default);
    void Update(RagSource ragSource);
    void Delete(RagSource ragSource);
}
