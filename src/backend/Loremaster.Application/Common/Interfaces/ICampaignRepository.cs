using Loremaster.Domain.Entities;

namespace Loremaster.Application.Common.Interfaces;

public interface ICampaignRepository
{
    Task<Campaign?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Campaign?> GetByIdWithMembersAsync(Guid id, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets a campaign by ID with full details including Owner, GameSystem, Members, and LoreEntities (for admin purposes).
    /// </summary>
    Task<Campaign?> GetByIdWithDetailsAsync(Guid id, CancellationToken cancellationToken = default);
    
    Task<Campaign?> GetByJoinCodeAsync(string joinCode, CancellationToken cancellationToken = default);
    Task<IEnumerable<Campaign>> GetByOwnerIdAsync(Guid ownerId, CancellationToken cancellationToken = default);
    Task<IEnumerable<Campaign>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<IEnumerable<Campaign>> GetActiveByGameSystemIdAsync(Guid gameSystemId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets all campaigns with full details including Owner, GameSystem, Members, and LoreEntities (for admin purposes).
    /// </summary>
    /// <param name="includeInactive">If true, includes inactive campaigns.</param>
    Task<IEnumerable<Campaign>> GetAllWithDetailsAsync(bool includeInactive = false, CancellationToken cancellationToken = default);
    
    Task<bool> ExistsByJoinCodeAsync(string joinCode, CancellationToken cancellationToken = default);
    Task<bool> IsUserMemberAsync(Guid campaignId, Guid userId, CancellationToken cancellationToken = default);
    Task AddAsync(Campaign campaign, CancellationToken cancellationToken = default);
    void Update(Campaign campaign);
    void Delete(Campaign campaign);
}
