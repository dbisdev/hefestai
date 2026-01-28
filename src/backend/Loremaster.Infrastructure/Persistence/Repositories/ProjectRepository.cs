using Loremaster.Application.Common.Interfaces;
using Loremaster.Domain.Entities;
using Loremaster.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Loremaster.Infrastructure.Persistence.Repositories;

public class ProjectRepository : IProjectRepository
{
    private readonly ApplicationDbContext _context;

    public ProjectRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Project?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Projects
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
    }

    public async Task<Project?> GetByIdWithOwnerAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Projects
            .Include(p => p.Owner)
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<Project>> GetByOwnerIdAsync(
        Guid ownerId, 
        ProjectStatus? status = null, 
        CancellationToken cancellationToken = default)
    {
        var query = _context.Projects
            .AsNoTracking()
            .Where(p => p.OwnerId == ownerId);

        if (status.HasValue)
        {
            query = query.Where(p => p.Status == status.Value);
        }

        return await query
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Projects
            .AnyAsync(p => p.Id == id, cancellationToken);
    }

    public async Task<bool> NameExistsForOwnerAsync(
        Guid ownerId, 
        string name, 
        Guid? excludeId = null, 
        CancellationToken cancellationToken = default)
    {
        var query = _context.Projects
            .Where(p => p.OwnerId == ownerId && p.Name.ToLower() == name.ToLower());

        if (excludeId.HasValue)
        {
            query = query.Where(p => p.Id != excludeId.Value);
        }

        return await query.AnyAsync(cancellationToken);
    }

    public async Task AddAsync(Project project, CancellationToken cancellationToken = default)
    {
        await _context.Projects.AddAsync(project, cancellationToken);
    }

    public Task UpdateAsync(Project project, CancellationToken cancellationToken = default)
    {
        _context.Projects.Update(project);
        return Task.CompletedTask;
    }

    public Task DeleteAsync(Project project, CancellationToken cancellationToken = default)
    {
        _context.Projects.Remove(project);
        return Task.CompletedTask;
    }

    public async Task<int> GetCountByOwnerAsync(Guid ownerId, CancellationToken cancellationToken = default)
    {
        return await _context.Projects
            .CountAsync(p => p.OwnerId == ownerId, cancellationToken);
    }
}
