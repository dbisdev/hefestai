using Loremaster.Application.Common.Interfaces;
using Loremaster.Domain.Common;
using Loremaster.Domain.Entities;
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

    public DbSet<User> Users => Set<User>();
    public DbSet<Project> Projects => Set<Project>();
    public DbSet<WorldEntity> WorldEntities => Set<WorldEntity>();
    public DbSet<Document> Documents => Set<Document>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Apply all configurations from assembly
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);

        // PostgreSQL specific settings
        modelBuilder.HasPostgresExtension("uuid-ossp");
        modelBuilder.HasPostgresExtension("vector"); // pgvector extension

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
