using Loremaster.Application.Common.Interfaces;
using Loremaster.Domain.Entities;
using Loremaster.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Loremaster.Infrastructure.Persistence.Repositories;

/// <summary>
/// Repository implementation for EntityTemplate entity.
/// </summary>
public class EntityTemplateRepository : IEntityTemplateRepository
{
    private readonly ApplicationDbContext _context;

    public EntityTemplateRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    /// <inheritdoc />
    public async Task<EntityTemplate?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.EntityTemplates
            .Include(et => et.GameSystem)
            .Include(et => et.Owner)
            .FirstOrDefaultAsync(et => et.Id == id, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<EntityTemplate?> GetByIdAsync(Guid id, Guid ownerId, CancellationToken cancellationToken = default)
    {
        return await _context.EntityTemplates
            .Include(et => et.GameSystem)
            .Include(et => et.Owner)
            .FirstOrDefaultAsync(et => et.Id == id && et.OwnerId == ownerId, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<EntityTemplate>> GetByGameSystemIdAsync(
        Guid gameSystemId, 
        Guid ownerId,
        CancellationToken cancellationToken = default)
    {
        return await _context.EntityTemplates
            .Include(et => et.GameSystem)
            .Where(et => et.GameSystemId == gameSystemId && et.OwnerId == ownerId)
            .OrderBy(et => et.DisplayName)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<EntityTemplate>> GetConfirmedByGameSystemIdAsync(
        Guid gameSystemId, 
        Guid ownerId,
        CancellationToken cancellationToken = default)
    {
        return await _context.EntityTemplates
            .Include(et => et.GameSystem)
            .Where(et => 
                et.GameSystemId == gameSystemId && 
                et.OwnerId == ownerId &&
                et.Status == TemplateStatus.Confirmed)
            .OrderBy(et => et.DisplayName)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<EntityTemplate>> GetByStatusAsync(
        Guid gameSystemId, 
        Guid ownerId,
        TemplateStatus status,
        CancellationToken cancellationToken = default)
    {
        return await _context.EntityTemplates
            .Include(et => et.GameSystem)
            .Where(et => 
                et.GameSystemId == gameSystemId && 
                et.OwnerId == ownerId &&
                et.Status == status)
            .OrderBy(et => et.DisplayName)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<EntityTemplate?> GetConfirmedTemplateForEntityTypeAsync(
        Guid gameSystemId,
        Guid ownerId,
        string entityTypeName,
        CancellationToken cancellationToken = default)
    {
        var normalizedTypeName = EntityTemplate.NormalizeEntityTypeName(entityTypeName);
        
        return await _context.EntityTemplates
            .Include(et => et.GameSystem)
            .FirstOrDefaultAsync(et => 
                et.GameSystemId == gameSystemId && 
                et.OwnerId == ownerId &&
                et.EntityTypeName == normalizedTypeName &&
                et.Status == TemplateStatus.Confirmed, 
                cancellationToken);
    }

    /// <inheritdoc />
    public async Task<bool> HasConfirmedTemplateAsync(
        Guid gameSystemId,
        Guid ownerId,
        string entityTypeName,
        CancellationToken cancellationToken = default)
    {
        var normalizedTypeName = EntityTemplate.NormalizeEntityTypeName(entityTypeName);
        
        return await _context.EntityTemplates
            .AnyAsync(et => 
                et.GameSystemId == gameSystemId && 
                et.OwnerId == ownerId &&
                et.EntityTypeName == normalizedTypeName &&
                et.Status == TemplateStatus.Confirmed, 
                cancellationToken);
    }

    /// <inheritdoc />
    public async Task<bool> ExistsByEntityTypeNameAsync(
        Guid gameSystemId,
        Guid ownerId,
        string entityTypeName,
        CancellationToken cancellationToken = default)
    {
        var normalizedTypeName = EntityTemplate.NormalizeEntityTypeName(entityTypeName);
        
        return await _context.EntityTemplates
            .AnyAsync(et => 
                et.GameSystemId == gameSystemId && 
                et.OwnerId == ownerId &&
                et.EntityTypeName == normalizedTypeName, 
                cancellationToken);
    }

    /// <inheritdoc />
    public async Task AddAsync(EntityTemplate template, CancellationToken cancellationToken = default)
    {
        await _context.EntityTemplates.AddAsync(template, cancellationToken);
    }

    /// <inheritdoc />
    public async Task AddRangeAsync(IEnumerable<EntityTemplate> templates, CancellationToken cancellationToken = default)
    {
        await _context.EntityTemplates.AddRangeAsync(templates, cancellationToken);
    }

    /// <inheritdoc />
    public void Update(EntityTemplate template)
    {
        _context.EntityTemplates.Update(template);
    }

    /// <inheritdoc />
    public void Delete(EntityTemplate template)
    {
        _context.EntityTemplates.Remove(template);
    }
}
