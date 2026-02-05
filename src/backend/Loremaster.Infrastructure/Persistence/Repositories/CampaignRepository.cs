using Loremaster.Application.Common.Interfaces;
using Loremaster.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Loremaster.Infrastructure.Persistence.Repositories;

public class CampaignRepository : ICampaignRepository
{
    private readonly ApplicationDbContext _context;

    public CampaignRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Campaign?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Campaigns
            .Include(c => c.GameSystem)
            .FirstOrDefaultAsync(c => c.Id == id && c.DeletedAt == null, cancellationToken);
    }

    public async Task<Campaign?> GetByIdWithMembersAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Campaigns
            .Include(c => c.GameSystem)
            .Include(c => c.Members)
                .ThenInclude(m => m.User)
            .FirstOrDefaultAsync(c => c.Id == id && c.DeletedAt == null, cancellationToken);
    }

    /// <summary>
    /// Gets a campaign by ID with full details including Owner, GameSystem, Members, and LoreEntities (for admin purposes).
    /// </summary>
    public async Task<Campaign?> GetByIdWithDetailsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Campaigns
            .Include(c => c.Owner)
            .Include(c => c.GameSystem)
            .Include(c => c.Members)
                .ThenInclude(m => m.User)
            .Include(c => c.LoreEntities)
            .FirstOrDefaultAsync(c => c.Id == id && c.DeletedAt == null, cancellationToken);
    }

    public async Task<Campaign?> GetByJoinCodeAsync(string joinCode, CancellationToken cancellationToken = default)
    {
        var normalizedCode = joinCode.ToUpperInvariant().Trim();
        return await _context.Campaigns
            .Include(c => c.GameSystem)
            .FirstOrDefaultAsync(c => c.JoinCode == normalizedCode && c.DeletedAt == null, cancellationToken);
    }

    public async Task<IEnumerable<Campaign>> GetByOwnerIdAsync(Guid ownerId, CancellationToken cancellationToken = default)
    {
        return await _context.Campaigns
            .Include(c => c.GameSystem)
            .Where(c => c.OwnerId == ownerId && c.DeletedAt == null)
            .OrderByDescending(c => c.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<Campaign>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await _context.Campaigns
            .Include(c => c.GameSystem)
            .Where(c => c.DeletedAt == null && c.Members.Any(m => m.UserId == userId))
            .OrderByDescending(c => c.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<Campaign>> GetActiveByGameSystemIdAsync(Guid gameSystemId, CancellationToken cancellationToken = default)
    {
        return await _context.Campaigns
            .Where(c => c.GameSystemId == gameSystemId && c.IsActive && c.DeletedAt == null)
            .OrderByDescending(c => c.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Gets all campaigns with full details including Owner, GameSystem, Members, and LoreEntities (for admin purposes).
    /// </summary>
    /// <param name="includeInactive">If true, includes inactive campaigns.</param>
    public async Task<IEnumerable<Campaign>> GetAllWithDetailsAsync(bool includeInactive = false, CancellationToken cancellationToken = default)
    {
        var query = _context.Campaigns
            .Include(c => c.Owner)
            .Include(c => c.GameSystem)
            .Include(c => c.Members)
            .Include(c => c.LoreEntities)
            .Where(c => c.DeletedAt == null);

        if (!includeInactive)
        {
            query = query.Where(c => c.IsActive);
        }

        return await query
            .OrderByDescending(c => c.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> ExistsByJoinCodeAsync(string joinCode, CancellationToken cancellationToken = default)
    {
        var normalizedCode = joinCode.ToUpperInvariant().Trim();
        return await _context.Campaigns
            .AnyAsync(c => c.JoinCode == normalizedCode && c.DeletedAt == null, cancellationToken);
    }

    public async Task<bool> IsUserMemberAsync(Guid campaignId, Guid userId, CancellationToken cancellationToken = default)
    {
        return await _context.CampaignMembers
            .AnyAsync(m => m.CampaignId == campaignId && m.UserId == userId, cancellationToken);
    }

    public async Task AddAsync(Campaign campaign, CancellationToken cancellationToken = default)
    {
        await _context.Campaigns.AddAsync(campaign, cancellationToken);
    }

    public void Update(Campaign campaign)
    {
        _context.Campaigns.Update(campaign);
    }

    public void Delete(Campaign campaign)
    {
        campaign.SoftDelete();
    }
}
