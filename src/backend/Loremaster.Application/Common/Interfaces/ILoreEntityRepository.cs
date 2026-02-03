using Loremaster.Domain.Entities;
using Loremaster.Domain.Enums;

namespace Loremaster.Application.Common.Interfaces;

public interface ILoreEntityRepository
{
    Task<LoreEntity?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<LoreEntity?> GetByIdWithRelationshipsAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IEnumerable<LoreEntity>> GetByCampaignIdAsync(Guid campaignId, CancellationToken cancellationToken = default);
    Task<IEnumerable<LoreEntity>> GetByCampaignAndTypeAsync(Guid campaignId, string entityType, CancellationToken cancellationToken = default);
    Task<IEnumerable<LoreEntity>> GetByOwnerIdAsync(Guid ownerId, CancellationToken cancellationToken = default);
    Task<IEnumerable<LoreEntity>> GetVisibleToCampaignMemberAsync(Guid campaignId, Guid userId, bool isMaster, CancellationToken cancellationToken = default);
    Task<IEnumerable<LoreEntity>> GetPlayerCharactersAsync(Guid campaignId, CancellationToken cancellationToken = default);
    Task<IEnumerable<LoreEntity>> GetTemplatesAsync(Guid campaignId, string? entityType = null, CancellationToken cancellationToken = default);
    Task<IEnumerable<LoreEntity>> GetByGenerationRequestIdAsync(Guid generationRequestId, CancellationToken cancellationToken = default);
    Task<IEnumerable<LoreEntity>> SearchByNameAsync(Guid campaignId, string searchTerm, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Counts entities of a specific type within a game system (across all campaigns).
    /// Used to check if a template can be safely deleted.
    /// </summary>
    /// <param name="gameSystemId">The game system ID.</param>
    /// <param name="entityType">The entity type name (normalized).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The count of entities using this entity type.</returns>
    Task<int> CountByEntityTypeAsync(Guid gameSystemId, string entityType, CancellationToken cancellationToken = default);
    
    Task AddAsync(LoreEntity entity, CancellationToken cancellationToken = default);
    void Update(LoreEntity entity);
    void Delete(LoreEntity entity);
}
