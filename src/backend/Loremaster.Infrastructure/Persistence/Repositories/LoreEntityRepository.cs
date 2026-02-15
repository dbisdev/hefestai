using Loremaster.Application.Common.Interfaces;
using Loremaster.Domain.Entities;
using Loremaster.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Loremaster.Infrastructure.Persistence.Repositories;

public class LoreEntityRepository : ILoreEntityRepository
{
    private readonly ApplicationDbContext _context;

    public LoreEntityRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<LoreEntity?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.LoreEntities
            .Include(e => e.Owner)
            .FirstOrDefaultAsync(e => e.Id == id && e.DeletedAt == null, cancellationToken);
    }

    public async Task<LoreEntity?> GetByIdWithRelationshipsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.LoreEntities
            .Include(e => e.Owner)
            .Include(e => e.OutgoingRelationships)
                .ThenInclude(r => r.TargetEntity)
            .Include(e => e.IncomingRelationships)
                .ThenInclude(r => r.SourceEntity)
            .FirstOrDefaultAsync(e => e.Id == id && e.DeletedAt == null, cancellationToken);
    }

    public async Task<IEnumerable<LoreEntity>> GetByCampaignIdAsync(Guid campaignId, CancellationToken cancellationToken = default)
    {
        return await _context.LoreEntities
            .Include(e => e.Owner)
            .Where(e => e.CampaignId == campaignId && e.DeletedAt == null)
            .OrderByDescending(e => e.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<LoreEntity>> GetByCampaignAndTypeAsync(Guid campaignId, string entityType, CancellationToken cancellationToken = default)
    {
        var normalizedType = entityType.ToLowerInvariant().Trim();
        return await _context.LoreEntities
            .Include(e => e.Owner)
            .Where(e => e.CampaignId == campaignId && e.EntityType == normalizedType && e.DeletedAt == null)
            .OrderByDescending(e => e.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<LoreEntity>> GetByOwnerIdAsync(Guid ownerId, CancellationToken cancellationToken = default)
    {
        return await _context.LoreEntities
            .Include(e => e.Campaign)
            .Where(e => e.OwnerId == ownerId && e.DeletedAt == null)
            .OrderByDescending(e => e.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<LoreEntity>> GetVisibleToCampaignMemberAsync(
        Guid campaignId, 
        Guid userId, 
        bool isMaster, 
        CancellationToken cancellationToken = default)
    {
        var query = _context.LoreEntities
            .Include(e => e.Owner)
            .Where(e => e.CampaignId == campaignId && e.DeletedAt == null);

        // Apply visibility filters
        // Both Masters and Players follow the same visibility rules:
        // - Own entities: always visible
        // - Draft: owner only
        // - Private: owner only  
        // - Campaign: all campaign members
        // - Public: everyone
        query = query.Where(e => 
            e.OwnerId == userId || // Own entities (includes Draft and Private)
            e.Visibility == VisibilityLevel.Campaign ||
            e.Visibility == VisibilityLevel.Public);

        return await query
            .OrderByDescending(e => e.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<LoreEntity>> GetPlayerCharactersAsync(Guid campaignId, CancellationToken cancellationToken = default)
    {
        return await _context.LoreEntities
            .Include(e => e.Owner)
            .Where(e => e.CampaignId == campaignId && 
                        e.EntityType == "character" && 
                        e.OwnershipType == OwnershipType.Player && 
                        e.DeletedAt == null)
            .OrderBy(e => e.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<LoreEntity>> GetTemplatesAsync(Guid campaignId, string? entityType = null, CancellationToken cancellationToken = default)
    {
        var query = _context.LoreEntities
            .Where(e => e.CampaignId == campaignId && e.IsTemplate && e.DeletedAt == null);

        if (!string.IsNullOrEmpty(entityType))
        {
            var normalizedType = entityType.ToLowerInvariant().Trim();
            query = query.Where(e => e.EntityType == normalizedType);
        }

        return await query
            .OrderBy(e => e.EntityType)
            .ThenBy(e => e.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<LoreEntity>> GetByGenerationRequestIdAsync(Guid generationRequestId, CancellationToken cancellationToken = default)
    {
        return await _context.LoreEntities
            .Where(e => e.GenerationRequestId == generationRequestId && e.DeletedAt == null)
            .OrderBy(e => e.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<LoreEntity>> SearchByNameAsync(Guid campaignId, string searchTerm, CancellationToken cancellationToken = default)
    {
        var normalizedSearch = searchTerm.ToLowerInvariant().Trim();
        return await _context.LoreEntities
            .Include(e => e.Owner)
            .Where(e => e.CampaignId == campaignId && 
                        e.DeletedAt == null &&
                        EF.Functions.ILike(e.Name, $"%{normalizedSearch}%"))
            .OrderBy(e => e.Name)
            .Take(50)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<int> CountByEntityTypeAsync(Guid gameSystemId, string entityType, CancellationToken cancellationToken = default)
    {
        var normalizedType = entityType.ToLowerInvariant().Trim();
        return await _context.LoreEntities
            .Include(e => e.Campaign)
            .Where(e => e.Campaign.GameSystemId == gameSystemId && 
                        e.EntityType == normalizedType && 
                        e.DeletedAt == null)
            .CountAsync(cancellationToken);
    }

    public async Task AddAsync(LoreEntity entity, CancellationToken cancellationToken = default)
    {
        await _context.LoreEntities.AddAsync(entity, cancellationToken);
    }

    public void Update(LoreEntity entity)
    {
        _context.LoreEntities.Update(entity);
    }

    public void Delete(LoreEntity entity)
    {
        entity.SoftDelete();
    }
}
