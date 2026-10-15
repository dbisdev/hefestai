using Loremaster.Application.Common.Interfaces;
using Loremaster.Domain.Entities;
using Loremaster.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Loremaster.Infrastructure.Persistence.Repositories;

public class GameSystemRepository : IGameSystemRepository
{
    private readonly ApplicationDbContext _context;

    public GameSystemRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<GameSystem?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.GameSystems
            .FirstOrDefaultAsync(g => g.Id == id, cancellationToken);
    }

    public async Task<GameSystem?> GetByCodeAsync(string code, CancellationToken cancellationToken = default)
    {
        var normalizedCode = code.ToLowerInvariant().Trim();
        return await _context.GameSystems
            .FirstOrDefaultAsync(g => g.Code == normalizedCode, cancellationToken);
    }

    public async Task<IEnumerable<GameSystem>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _context.GameSystems
            .OrderBy(g => g.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<GameSystem>> GetActiveAsync(CancellationToken cancellationToken = default)
    {
        return await _context.GameSystems
            .Where(g => g.IsActive)
            .OrderBy(g => g.Name)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<GameSystem>> GetActiveForUserAsync(
        Guid? userId,
        bool includeAll,
        bool isPlayer = false,
        CancellationToken cancellationToken = default)
    {
        // Join GameSystems with Users to filter by the owner's role.
        var query = from gs in _context.GameSystems
                    join u in _context.Users on gs.OwnerId equals u.Id
                    where gs.IsActive
                    select new { GameSystem = gs, OwnerRole = u.Role };

        if (includeAll)
        {
            // Admin: sees every active system regardless of owner.
        }
        else if (isPlayer && userId.HasValue)
        {
            // Player: Admin-owned systems + systems owned by Masters of the Player's campaigns.
            // Sub-query: collect all Master userIds from campaigns the player belongs to.
            var campaignMasterIds =
                from cm in _context.CampaignMembers
                join masterMembership in _context.CampaignMembers
                    on cm.CampaignId equals masterMembership.CampaignId
                where cm.UserId == userId.Value
                   && cm.Campaign.DeletedAt == null
                   && masterMembership.Role == CampaignRole.Master
                select masterMembership.UserId;

            query = query.Where(x =>
                x.OwnerRole == UserRole.Admin ||
                campaignMasterIds.Contains(x.GameSystem.OwnerId));
        }
        else if (userId.HasValue)
        {
            // Master: Admin-owned systems + own systems.
            query = query.Where(x => x.OwnerRole == UserRole.Admin || x.GameSystem.OwnerId == userId.Value);
        }
        else
        {
            // Fallback: only Admin-owned systems.
            query = query.Where(x => x.OwnerRole == UserRole.Admin);
        }

        var results = await query
            .OrderBy(x => x.GameSystem.Name)
            .Select(x => x.GameSystem)
            .ToListAsync(cancellationToken);

        return results;
    }

    public async Task<bool> ExistsByCodeAsync(string code, CancellationToken cancellationToken = default)
    {
        var normalizedCode = code.ToLowerInvariant().Trim();
        return await _context.GameSystems
            .AnyAsync(g => g.Code == normalizedCode, cancellationToken);
    }

    /// <summary>
    /// Returns true if a game system with the given ID exists.
    /// </summary>
    public async Task<bool> ExistsByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.GameSystems
            .AnyAsync(g => g.Id == id, cancellationToken);
    }

    public async Task AddAsync(GameSystem gameSystem, CancellationToken cancellationToken = default)
    {
        await _context.GameSystems.AddAsync(gameSystem, cancellationToken);
    }

    public void Update(GameSystem gameSystem)
    {
        _context.GameSystems.Update(gameSystem);
    }

    public void Delete(GameSystem gameSystem)
    {
        _context.GameSystems.Remove(gameSystem);
    }
}
