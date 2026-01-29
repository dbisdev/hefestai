using Loremaster.Application.Common.Interfaces;
using Loremaster.Domain.Entities;
using Loremaster.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Loremaster.Infrastructure.Persistence.Repositories;

public class RagSourceRepository : IRagSourceRepository
{
    private readonly ApplicationDbContext _context;

    public RagSourceRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<RagSource?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.RagSources
            .Include(r => r.GameSystem)
            .FirstOrDefaultAsync(r => r.Id == id, cancellationToken);
    }

    public async Task<IEnumerable<RagSource>> GetByGameSystemIdAsync(Guid gameSystemId, CancellationToken cancellationToken = default)
    {
        return await _context.RagSources
            .Where(r => r.GameSystemId == gameSystemId)
            .OrderBy(r => r.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<RagSource>> GetActiveByGameSystemIdAsync(Guid gameSystemId, CancellationToken cancellationToken = default)
    {
        return await _context.RagSources
            .Where(r => r.GameSystemId == gameSystemId && r.IsActive)
            .OrderBy(r => r.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<RagSource>> GetByTypeAsync(RagSourceType sourceType, CancellationToken cancellationToken = default)
    {
        return await _context.RagSources
            .Include(r => r.GameSystem)
            .Where(r => r.SourceType == sourceType)
            .OrderBy(r => r.GameSystem.Name)
            .ThenBy(r => r.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<RagSource?> GetByContentHashAsync(string contentHash, CancellationToken cancellationToken = default)
    {
        return await _context.RagSources
            .FirstOrDefaultAsync(r => r.ContentHash == contentHash, cancellationToken);
    }

    public async Task AddAsync(RagSource ragSource, CancellationToken cancellationToken = default)
    {
        await _context.RagSources.AddAsync(ragSource, cancellationToken);
    }

    public void Update(RagSource ragSource)
    {
        _context.RagSources.Update(ragSource);
    }

    public void Delete(RagSource ragSource)
    {
        _context.RagSources.Remove(ragSource);
    }
}
