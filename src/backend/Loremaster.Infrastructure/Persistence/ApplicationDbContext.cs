using Loremaster.Application.Common.Interfaces;
using Loremaster.Domain.Common;
using Loremaster.Domain.Entities;
using Loremaster.Domain.Enums;
using Loremaster.Infrastructure.Persistence.Interceptors;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Loremaster.Infrastructure.Persistence;

public class ApplicationDbContext : DbContext, IApplicationDbContext, IUnitOfWork
{
    private readonly AuditableEntityInterceptor _auditableEntityInterceptor;
    private readonly IMediator? _mediator;

    public ApplicationDbContext(
        DbContextOptions<ApplicationDbContext> options,
        AuditableEntityInterceptor auditableEntityInterceptor,
        IMediator? mediator = null)
        : base(options)
    {
        _auditableEntityInterceptor = auditableEntityInterceptor;
        _mediator = mediator;
    }

    // Core entities
    public DbSet<User> Users => Set<User>();
    public DbSet<GameSystem> GameSystems => Set<GameSystem>();
    public DbSet<Campaign> Campaigns => Set<Campaign>();
    public DbSet<CampaignMember> CampaignMembers => Set<CampaignMember>();
    
    // Lore entities
    public DbSet<LoreEntity> LoreEntities => Set<LoreEntity>();
    public DbSet<LoreEntityRelationship> LoreEntityRelationships => Set<LoreEntityRelationship>();
    public DbSet<LoreEntityImport> LoreEntityImports => Set<LoreEntityImport>();
    
    // Generation/AI entities
    public DbSet<GenerationRequest> GenerationRequests => Set<GenerationRequest>();
    public DbSet<GenerationResult> GenerationResults => Set<GenerationResult>();
    public DbSet<GenerationResultSource> GenerationResultSources => Set<GenerationResultSource>();
    public DbSet<RagSource> RagSources => Set<RagSource>();
    
    // Legacy entities (to be migrated/removed)
    public DbSet<Project> Projects => Set<Project>();
    public DbSet<Document> Documents => Set<Document>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Apply all configurations from assembly
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);

        // PostgreSQL specific settings
        modelBuilder.HasPostgresExtension("uuid-ossp");
        modelBuilder.HasPostgresExtension("vector"); // pgvector extension
        
        // Register PostgreSQL enum types
        modelBuilder.HasPostgresEnum<UserRole>("user_role");

        base.OnModelCreating(modelBuilder);
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.AddInterceptors(_auditableEntityInterceptor);
        
        // Enable sensitive data logging in development
        #if DEBUG
        optionsBuilder.EnableSensitiveDataLogging();
        optionsBuilder.EnableDetailedErrors();
        #endif

        base.OnConfiguring(optionsBuilder);
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        // Dispatch domain events before saving
        await DispatchDomainEventsAsync(cancellationToken);

        return await base.SaveChangesAsync(cancellationToken);
    }

    private async Task DispatchDomainEventsAsync(CancellationToken cancellationToken)
    {
        if (_mediator == null) return;

        var entities = ChangeTracker
            .Entries<BaseEntity>()
            .Where(e => e.Entity.DomainEvents.Any())
            .Select(e => e.Entity)
            .ToList();

        var domainEvents = entities
            .SelectMany(e => e.DomainEvents)
            .ToList();

        entities.ForEach(e => e.ClearDomainEvents());

        foreach (var domainEvent in domainEvents)
        {
            await _mediator.Publish(domainEvent, cancellationToken);
        }
    }
}
