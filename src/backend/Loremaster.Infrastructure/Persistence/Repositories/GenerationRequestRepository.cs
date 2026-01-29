using Loremaster.Application.Common.Interfaces;
using Loremaster.Domain.Entities;
using Loremaster.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Loremaster.Infrastructure.Persistence.Repositories;

public class GenerationRequestRepository : IGenerationRequestRepository
{
    private readonly ApplicationDbContext _context;

    public GenerationRequestRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<GenerationRequest?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.GenerationRequests
            .Include(r => r.User)
            .FirstOrDefaultAsync(r => r.Id == id, cancellationToken);
    }

    public async Task<GenerationRequest?> GetByIdWithResultsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.GenerationRequests
            .Include(r => r.User)
            .Include(r => r.Results)
                .ThenInclude(res => res.Sources)
                    .ThenInclude(s => s.RagSource)
            .FirstOrDefaultAsync(r => r.Id == id, cancellationToken);
    }

    public async Task<IEnumerable<GenerationRequest>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await _context.GenerationRequests
            .Where(r => r.UserId == userId)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<GenerationRequest>> GetByCampaignIdAsync(Guid campaignId, CancellationToken cancellationToken = default)
    {
        return await _context.GenerationRequests
            .Include(r => r.User)
            .Where(r => r.CampaignId == campaignId)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<GenerationRequest>> GetByStatusAsync(GenerationStatus status, CancellationToken cancellationToken = default)
    {
        return await _context.GenerationRequests
            .Where(r => r.Status == status)
            .OrderBy(r => r.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<GenerationRequest>> GetPendingAsync(int limit = 10, CancellationToken cancellationToken = default)
    {
        return await _context.GenerationRequests
            .Where(r => r.Status == GenerationStatus.Pending)
            .OrderBy(r => r.CreatedAt)
            .Take(limit)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<GenerationRequest>> GetRecentByUserAsync(Guid userId, int limit = 20, CancellationToken cancellationToken = default)
    {
        return await _context.GenerationRequests
            .Include(r => r.Campaign)
            .Where(r => r.UserId == userId)
            .OrderByDescending(r => r.CreatedAt)
            .Take(limit)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(GenerationRequest request, CancellationToken cancellationToken = default)
    {
        await _context.GenerationRequests.AddAsync(request, cancellationToken);
    }

    public void Update(GenerationRequest request)
    {
        _context.GenerationRequests.Update(request);
    }
}
