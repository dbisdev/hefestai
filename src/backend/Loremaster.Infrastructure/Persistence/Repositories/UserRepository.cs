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
            .FirstOrDefaultAsync(u => u.Id == id, cancellationToken);
    }

    public async Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        var normalizedEmail = email.ToLowerInvariant().Trim();
        return await _context.Users
            .FirstOrDefaultAsync(u => u.Email == normalizedEmail, cancellationToken);
    }

    public async Task<User?> GetByInvitationCodeAsync(string invitationCode, CancellationToken cancellationToken = default)
    {
        var normalizedCode = invitationCode.ToUpperInvariant().Trim();
        return await _context.Users
            .FirstOrDefaultAsync(u => u.InvitationCode == normalizedCode && u.Role == UserRole.Master, cancellationToken);
    }

    public async Task<IEnumerable<User>> GetMastersAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Users
            .Where(u => u.Role == UserRole.Master && u.IsActive)
            .OrderBy(u => u.DisplayName ?? u.Email)
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> ExistsByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        var normalizedEmail = email.ToLowerInvariant().Trim();
        return await _context.Users
            .AnyAsync(u => u.Email == normalizedEmail, cancellationToken);
    }

    public async Task AddAsync(User user, CancellationToken cancellationToken = default)
    {
        await _context.Users.AddAsync(user, cancellationToken);
    }

    public Task UpdateAsync(User user, CancellationToken cancellationToken = default)
    {
        _context.Users.Update(user);
        return Task.CompletedTask;
    }

    public Task DeleteAsync(User user, CancellationToken cancellationToken = default)
    {
        _context.Users.Remove(user);
        return Task.CompletedTask;
    }
}
