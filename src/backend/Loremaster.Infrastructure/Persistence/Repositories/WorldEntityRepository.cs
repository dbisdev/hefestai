using Loremaster.Application.Common.Interfaces;
using Loremaster.Domain.Entities;
using Loremaster.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Loremaster.Infrastructure.Persistence.Repositories;

public class WorldEntityRepository : IWorldEntityRepository
{
    private readonly ApplicationDbContext _context;

    public WorldEntityRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<WorldEntity?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.WorldEntities
            .Include(e => e.Creator)
            .FirstOrDefaultAsync(e => e.Id == id, cancellationToken);
    }

    public async Task<IEnumerable<WorldEntity>> GetByCreatorIdAsync(Guid creatorId, CancellationToken cancellationToken = default)
    {
        return await _context.WorldEntities
            .Where(e => e.CreatorId == creatorId)
            .OrderByDescending(e => e.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<WorldEntity>> GetByCreatorIdAndCategoryAsync(Guid creatorId, EntityCategory category, CancellationToken cancellationToken = default)
    {
        return await _context.WorldEntities
            .Where(e => e.CreatorId == creatorId && e.Category == category)
            .OrderByDescending(e => e.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<WorldEntity>> GetVisibleEntitiesAsync(Guid userId, Guid? masterId, CancellationToken cancellationToken = default)
    {
        // If masterId is provided (player), show master's entities
        // If masterId is null (master), show own entities
        var targetCreatorId = masterId ?? userId;
        
        return await _context.WorldEntities
            .Where(e => e.CreatorId == targetCreatorId)
            .OrderByDescending(e => e.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(WorldEntity entity, CancellationToken cancellationToken = default)
    {
        await _context.WorldEntities.AddAsync(entity, cancellationToken);
    }

    public void Update(WorldEntity entity)
    {
        _context.WorldEntities.Update(entity);
    }

    public void Delete(WorldEntity entity)
    {
        _context.WorldEntities.Remove(entity);
    }
}
