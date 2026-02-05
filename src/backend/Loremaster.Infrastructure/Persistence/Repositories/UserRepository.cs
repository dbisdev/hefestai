using Loremaster.Application.Common.Interfaces;
using Loremaster.Domain.Entities;
using Loremaster.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Loremaster.Infrastructure.Persistence.Repositories;

public class UserRepository : IUserRepository
{
    private readonly ApplicationDbContext _context;

    public UserRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Users
            .FirstOrDefaultAsync(u => u.Id == id && u.DeletedAt == null, cancellationToken);
    }

    public async Task<User?> GetByExternalIdAsync(string externalId, CancellationToken cancellationToken = default)
    {
        var normalizedExternalId = externalId.Trim();
        return await _context.Users
            .FirstOrDefaultAsync(u => u.ExternalId == normalizedExternalId && u.DeletedAt == null, cancellationToken);
    }

    public async Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        var normalizedEmail = email.ToLowerInvariant().Trim();
        return await _context.Users
            .FirstOrDefaultAsync(u => u.Email == normalizedEmail && u.DeletedAt == null, cancellationToken);
    }

    public async Task<User?> GetByInvitationCodeAsync(string invitationCode, CancellationToken cancellationToken = default)
    {
        var normalizedCode = invitationCode.ToUpperInvariant().Trim();
        return await _context.Users
            .FirstOrDefaultAsync(u => u.InvitationCode == normalizedCode && u.DeletedAt == null && u.IsActive, cancellationToken);
    }

    public async Task<IEnumerable<User>> GetByRoleAsync(UserRole role, CancellationToken cancellationToken = default)
    {
        return await _context.Users
            .Where(u => u.Role == role && u.DeletedAt == null)
            .OrderBy(u => u.DisplayName)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<User>> GetAllActiveAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Users
            .Where(u => u.DeletedAt == null && u.IsActive)
            .Include(u => u.OwnedCampaigns)
            .Include(u => u.CampaignMemberships)
            .OrderBy(u => u.DisplayName)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Gets all users including inactive ones (for admin purposes).
    /// </summary>
    public async Task<IEnumerable<User>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Users
            .Where(u => u.DeletedAt == null)
            .Include(u => u.OwnedCampaigns)
            .Include(u => u.CampaignMemberships)
            .OrderBy(u => u.DisplayName)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<User>> GetPlayersByMasterIdAsync(Guid masterId, CancellationToken cancellationToken = default)
    {
        return await _context.Users
            .Where(u => u.MasterId == masterId && u.DeletedAt == null)
            .OrderBy(u => u.DisplayName)
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> ExistsByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        var normalizedEmail = email.ToLowerInvariant().Trim();
        return await _context.Users
            .AnyAsync(u => u.Email == normalizedEmail && u.DeletedAt == null, cancellationToken);
    }

    public async Task<bool> ExistsByExternalIdAsync(string externalId, CancellationToken cancellationToken = default)
    {
        var normalizedExternalId = externalId.Trim();
        return await _context.Users
            .AnyAsync(u => u.ExternalId == normalizedExternalId && u.DeletedAt == null, cancellationToken);
    }

    public async Task AddAsync(User user, CancellationToken cancellationToken = default)
    {
        await _context.Users.AddAsync(user, cancellationToken);
    }

    public async Task UpdateAsync(User user, CancellationToken cancellationToken = default)
    {
        _context.Users.Update(user);
        await Task.CompletedTask; // Satisfy async signature - actual save happens via UnitOfWork
    }

    public void Update(User user)
    {
        _context.Users.Update(user);
    }

    public void Delete(User user)
    {
        user.SoftDelete();
    }
}
