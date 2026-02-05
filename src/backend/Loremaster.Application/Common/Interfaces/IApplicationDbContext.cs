using Loremaster.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Loremaster.Application.Common.Interfaces;

public interface IApplicationDbContext
{
    // Core entities
    DbSet<User> Users { get; }
    DbSet<GameSystem> GameSystems { get; }
    DbSet<Campaign> Campaigns { get; }
    DbSet<CampaignMember> CampaignMembers { get; }
    
    // Lore entities
    DbSet<LoreEntity> LoreEntities { get; }
    DbSet<LoreEntityRelationship> LoreEntityRelationships { get; }
    DbSet<LoreEntityImport> LoreEntityImports { get; }
    
    // Generation/AI entities
    DbSet<GenerationRequest> GenerationRequests { get; }
    DbSet<GenerationResult> GenerationResults { get; }
    DbSet<GenerationResultSource> GenerationResultSources { get; }
    DbSet<RagSource> RagSources { get; }
    
    // Legacy entities (to be migrated/removed)
    DbSet<Document> Documents { get; }
    
    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
}
