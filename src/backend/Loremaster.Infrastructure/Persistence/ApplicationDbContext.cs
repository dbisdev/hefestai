using Loremaster.Application.Common.Interfaces;
using Loremaster.Domain.Common;
using Loremaster.Domain.Entities;
using Loremaster.Domain.Enums;
using Loremaster.Infrastructure.Persistence.Interceptors;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Pgvector;
using System.Text.Json;

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
    
    // Entity Templates (EPIC 4)
    public DbSet<EntityTemplate> EntityTemplates => Set<EntityTemplate>();
    
    // Legacy entities (to be migrated/removed)
    public DbSet<Project> Projects => Set<Project>();
    public DbSet<Document> Documents => Set<Document>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Apply all configurations from assembly
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);

        // Check if we're using InMemory provider (for testing)
        var isInMemory = Database.IsInMemory();
        
        if (isInMemory)
        {
            // Add type converters for InMemory provider (JsonDocument, Vector)
            ConfigureInMemoryTypeConverters(modelBuilder);
        }
        else
        {
            // PostgreSQL specific settings
            modelBuilder.HasPostgresExtension("uuid-ossp");
            modelBuilder.HasPostgresExtension("vector"); // pgvector extension
            
            // Register PostgreSQL enum types in public schema
            modelBuilder.HasPostgresEnum<UserRole>("public", "user_role");
            modelBuilder.HasPostgresEnum<CampaignRole>("public", "campaign_role");
            modelBuilder.HasPostgresEnum<OwnershipType>("public", "ownership_type");
            modelBuilder.HasPostgresEnum<VisibilityLevel>("public", "visibility_level");
        }

        base.OnModelCreating(modelBuilder);
    }

    /// <summary>
    /// Configures value converters for types not supported by InMemory provider.
    /// Includes JsonDocument (stored as JSON strings) and Vector/pgvector (stored as comma-separated floats).
    /// </summary>
    private static void ConfigureInMemoryTypeConverters(ModelBuilder modelBuilder)
    {
        // JsonDocument converter - store as JSON string
        var jsonDocumentConverter = new ValueConverter<JsonDocument?, string?>(
            v => SerializeJsonDocument(v),
            v => DeserializeJsonDocument(v));

        // Campaign.Settings
        modelBuilder.Entity<Campaign>()
            .Property(c => c.Settings)
            .HasConversion(jsonDocumentConverter);

        // LoreEntity.Attributes and Metadata
        modelBuilder.Entity<LoreEntity>()
            .Property(e => e.Attributes)
            .HasConversion(jsonDocumentConverter);
        modelBuilder.Entity<LoreEntity>()
            .Property(e => e.Metadata)
            .HasConversion(jsonDocumentConverter);

        // GenerationRequest.InputParameters
        modelBuilder.Entity<GenerationRequest>()
            .Property(r => r.InputParameters)
            .HasConversion(jsonDocumentConverter);

        // GenerationResult properties
        modelBuilder.Entity<GenerationResult>()
            .Property(r => r.RawOutput)
            .HasConversion(jsonDocumentConverter);
        modelBuilder.Entity<GenerationResult>()
            .Property(r => r.StructuredOutput)
            .HasConversion(jsonDocumentConverter);
        modelBuilder.Entity<GenerationResult>()
            .Property(r => r.ModelParameters)
            .HasConversion(jsonDocumentConverter);
        modelBuilder.Entity<GenerationResult>()
            .Property(r => r.TokenUsage)
            .HasConversion(jsonDocumentConverter);

        // LoreEntityImport properties
        modelBuilder.Entity<LoreEntityImport>()
            .Property(i => i.ExtractionResult)
            .HasConversion(jsonDocumentConverter);
        modelBuilder.Entity<LoreEntityImport>()
            .Property(i => i.FieldMapping)
            .HasConversion(jsonDocumentConverter);

        // Vector (pgvector) converter - store as comma-separated floats
        var vectorConverter = new ValueConverter<Vector?, string?>(
            v => SerializeVector(v),
            v => DeserializeVector(v));

        // Document.Embedding
        modelBuilder.Entity<Document>()
            .Property(d => d.Embedding)
            .HasConversion(vectorConverter);
    }

    /// <summary>
    /// Serializes a JsonDocument to a string for InMemory database storage.
    /// </summary>
    private static string? SerializeJsonDocument(JsonDocument? document)
    {
        return document?.RootElement.GetRawText();
    }

    /// <summary>
    /// Deserializes a string to a JsonDocument for InMemory database retrieval.
    /// </summary>
    private static JsonDocument? DeserializeJsonDocument(string? json)
    {
        if (string.IsNullOrEmpty(json))
            return null;
        return JsonDocument.Parse(json);
    }

    /// <summary>
    /// Serializes a Vector to a comma-separated string for InMemory database storage.
    /// </summary>
    private static string? SerializeVector(Vector? vector)
    {
        if (vector == null)
            return null;
        return string.Join(",", vector.ToArray());
    }

    /// <summary>
    /// Deserializes a comma-separated string to a Vector for InMemory database retrieval.
    /// </summary>
    private static Vector? DeserializeVector(string? value)
    {
        if (string.IsNullOrEmpty(value))
            return null;
        var floats = value.Split(',').Select(float.Parse).ToArray();
        return new Vector(floats);
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
