using Loremaster.Domain.Entities;
using Loremaster.Domain.Enums;

namespace Loremaster.Application.Common.Interfaces;

public interface IGenerationRequestRepository
{
    Task<GenerationRequest?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<GenerationRequest?> GetByIdWithResultsAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IEnumerable<GenerationRequest>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<IEnumerable<GenerationRequest>> GetByCampaignIdAsync(Guid campaignId, CancellationToken cancellationToken = default);
    Task<IEnumerable<GenerationRequest>> GetByStatusAsync(GenerationStatus status, CancellationToken cancellationToken = default);
    Task<IEnumerable<GenerationRequest>> GetPendingAsync(int limit = 10, CancellationToken cancellationToken = default);
    Task<IEnumerable<GenerationRequest>> GetRecentByUserAsync(Guid userId, int limit = 20, CancellationToken cancellationToken = default);
    Task AddAsync(GenerationRequest request, CancellationToken cancellationToken = default);
    void Update(GenerationRequest request);
}
