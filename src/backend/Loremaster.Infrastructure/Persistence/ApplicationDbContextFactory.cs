using Loremaster.Application.Common.Interfaces;
using Loremaster.Domain.Enums;
using Loremaster.Infrastructure.Persistence.Interceptors;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using Npgsql;

namespace Loremaster.Infrastructure.Persistence;

/// <summary>
/// Factory for creating ApplicationDbContext at design time for EF Core migrations.
/// </summary>
public class ApplicationDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
{
    public ApplicationDbContext CreateDbContext(string[] args)
    {
        // Build configuration
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Path.Combine(Directory.GetCurrentDirectory(), "../Loremaster.Api"))
            .AddJsonFile("appsettings.json", optional: false)
            .AddJsonFile("appsettings.Development.json", optional: true)
            .Build();

        var connectionString = configuration.GetConnectionString("DefaultConnection");

        // Configure NpgsqlDataSource with PostgreSQL enum mappings
        var dataSourceBuilder = new NpgsqlDataSourceBuilder(connectionString);
        dataSourceBuilder.MapEnum<UserRole>("user_role");
        dataSourceBuilder.MapEnum<CampaignRole>("campaign_role");
        dataSourceBuilder.MapEnum<OwnershipType>("ownership_type");
        dataSourceBuilder.MapEnum<VisibilityLevel>("visibility_level");
        var dataSource = dataSourceBuilder.Build();

        var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();
        optionsBuilder.UseNpgsql(dataSource, b => 
            b.MigrationsAssembly(typeof(ApplicationDbContext).Assembly.FullName));

        // Create a dummy interceptor for design time
        var interceptor = new AuditableEntityInterceptor(
            new DesignTimeCurrentUserService(),
            new DesignTimeDateTimeProvider());

        return new ApplicationDbContext(optionsBuilder.Options, interceptor);
    }

    // Design time implementations
    private class DesignTimeCurrentUserService : ICurrentUserService
    {
        public Guid? UserId => null;
        public string? Email => null;
        public bool IsAuthenticated => false;
    }

    private class DesignTimeDateTimeProvider : IDateTimeProvider
    {
        public DateTime UtcNow => DateTime.UtcNow;
    }
}
