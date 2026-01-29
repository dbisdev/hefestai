using Loremaster.Domain.Entities;

namespace Loremaster.Application.Common.Interfaces;

public interface ICampaignRepository
{
    Task<Campaign?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Campaign?> GetByIdWithMembersAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Campaign?> GetByJoinCodeAsync(string joinCode, CancellationToken cancellationToken = default);
    Task<IEnumerable<Campaign>> GetByOwnerIdAsync(Guid ownerId, CancellationToken cancellationToken = default);
    Task<IEnumerable<Campaign>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<IEnumerable<Campaign>> GetActiveByGameSystemIdAsync(Guid gameSystemId, CancellationToken cancellationToken = default);
    Task<bool> ExistsByJoinCodeAsync(string joinCode, CancellationToken cancellationToken = default);
    Task<bool> IsUserMemberAsync(Guid campaignId, Guid userId, CancellationToken cancellationToken = default);
    Task AddAsync(Campaign campaign, CancellationToken cancellationToken = default);
    void Update(Campaign campaign);
    void Delete(Campaign campaign);
}
