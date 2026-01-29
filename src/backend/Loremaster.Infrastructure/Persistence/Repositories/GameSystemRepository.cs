using Loremaster.Application.Common.Interfaces;
using Loremaster.Domain.Entities;
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

    public async Task<bool> ExistsByCodeAsync(string code, CancellationToken cancellationToken = default)
    {
        var normalizedCode = code.ToLowerInvariant().Trim();
        return await _context.GameSystems
            .AnyAsync(g => g.Code == normalizedCode, cancellationToken);
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
