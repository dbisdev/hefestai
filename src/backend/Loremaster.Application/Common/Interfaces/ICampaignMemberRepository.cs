using Loremaster.Domain.Entities;
using Loremaster.Domain.Enums;

namespace Loremaster.Application.Common.Interfaces;

public interface ICampaignMemberRepository
{
    Task<CampaignMember?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<CampaignMember?> GetByCampaignAndUserAsync(Guid campaignId, Guid userId, CancellationToken cancellationToken = default);
    Task<IEnumerable<CampaignMember>> GetByCampaignIdAsync(Guid campaignId, CancellationToken cancellationToken = default);
    Task<IEnumerable<CampaignMember>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<IEnumerable<CampaignMember>> GetMastersByCampaignIdAsync(Guid campaignId, CancellationToken cancellationToken = default);
    Task<bool> IsMasterAsync(Guid campaignId, Guid userId, CancellationToken cancellationToken = default);
    Task<bool> IsMemberAsync(Guid campaignId, Guid userId, CancellationToken cancellationToken = default);
    Task<int> GetMemberCountAsync(Guid campaignId, CancellationToken cancellationToken = default);
    Task AddAsync(CampaignMember member, CancellationToken cancellationToken = default);
    void Update(CampaignMember member);
    void Delete(CampaignMember member);
}
