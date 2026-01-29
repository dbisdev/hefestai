using Loremaster.Application.Common.Interfaces;
using Loremaster.Domain.Entities;
using Loremaster.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Loremaster.Infrastructure.Persistence.Repositories;

public class CampaignMemberRepository : ICampaignMemberRepository
{
    private readonly ApplicationDbContext _context;

    public CampaignMemberRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<CampaignMember?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.CampaignMembers
            .Include(m => m.User)
            .Include(m => m.Campaign)
            .FirstOrDefaultAsync(m => m.Id == id, cancellationToken);
    }

    public async Task<CampaignMember?> GetByCampaignAndUserAsync(Guid campaignId, Guid userId, CancellationToken cancellationToken = default)
    {
        return await _context.CampaignMembers
            .Include(m => m.User)
            .Include(m => m.Campaign)
            .FirstOrDefaultAsync(m => m.CampaignId == campaignId && m.UserId == userId, cancellationToken);
    }

    public async Task<IEnumerable<CampaignMember>> GetByCampaignIdAsync(Guid campaignId, CancellationToken cancellationToken = default)
    {
        return await _context.CampaignMembers
            .Include(m => m.User)
            .Where(m => m.CampaignId == campaignId)
            .OrderBy(m => m.Role)
            .ThenBy(m => m.JoinedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<CampaignMember>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await _context.CampaignMembers
            .Include(m => m.Campaign)
                .ThenInclude(c => c.GameSystem)
            .Where(m => m.UserId == userId && m.Campaign.DeletedAt == null)
            .OrderByDescending(m => m.JoinedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<CampaignMember>> GetMastersByCampaignIdAsync(Guid campaignId, CancellationToken cancellationToken = default)
    {
        return await _context.CampaignMembers
            .Include(m => m.User)
            .Where(m => m.CampaignId == campaignId && m.Role == CampaignRole.Master)
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> IsMasterAsync(Guid campaignId, Guid userId, CancellationToken cancellationToken = default)
    {
        return await _context.CampaignMembers
            .AnyAsync(m => m.CampaignId == campaignId && m.UserId == userId && m.Role == CampaignRole.Master, cancellationToken);
    }

    public async Task<bool> IsMemberAsync(Guid campaignId, Guid userId, CancellationToken cancellationToken = default)
    {
        return await _context.CampaignMembers
            .AnyAsync(m => m.CampaignId == campaignId && m.UserId == userId, cancellationToken);
    }

    public async Task<int> GetMemberCountAsync(Guid campaignId, CancellationToken cancellationToken = default)
    {
        return await _context.CampaignMembers
            .CountAsync(m => m.CampaignId == campaignId, cancellationToken);
    }

    public async Task AddAsync(CampaignMember member, CancellationToken cancellationToken = default)
    {
        await _context.CampaignMembers.AddAsync(member, cancellationToken);
    }

    public void Update(CampaignMember member)
    {
        _context.CampaignMembers.Update(member);
    }

    public void Delete(CampaignMember member)
    {
        _context.CampaignMembers.Remove(member);
    }
}
