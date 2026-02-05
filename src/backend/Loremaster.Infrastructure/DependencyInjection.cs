using System.Text;
using Loremaster.Application.Common.Interfaces;
using Loremaster.Domain.Enums;
using Loremaster.Infrastructure.Identity;
using Loremaster.Infrastructure.Persistence;
using Loremaster.Infrastructure.Persistence.Interceptors;
using Loremaster.Infrastructure.Persistence.Repositories;
using Loremaster.Infrastructure.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Npgsql;
using Polly;
using Polly.Extensions.Http;

namespace Loremaster.Infrastructure;

public static class DependencyInjection
{
    /// <summary>
    /// Adds infrastructure services to the service collection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The configuration.</param>
    /// <param name="environmentName">The environment name (e.g., "Development", "Testing", "Production").</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddInfrastructureServices(
        this IServiceCollection services, 
        IConfiguration configuration,
        string environmentName)
    {
        var isDevelopment = environmentName == "Development";
        var isTesting = environmentName == "Testing";

        // Database
        services.AddScoped<AuditableEntityInterceptor>();
        
        // Skip PostgreSQL/Npgsql setup for testing environment
        // The CustomWebApplicationFactory will provide an InMemory database instead
        if (!isTesting)
        {
            // Use different connection strings based on environment
            var connectionString = isDevelopment 
                ? configuration.GetConnectionString("DefaultConnection") 
                : configuration.GetConnectionString("SupabaseConnection") 
                  ?? configuration.GetConnectionString("DefaultConnection");
            
            // Configure NpgsqlDataSource with PostgreSQL enum mappings and pgvector support
            var dataSourceBuilder = new NpgsqlDataSourceBuilder(connectionString);
            dataSourceBuilder.MapEnum<UserRole>("public.user_role");
            dataSourceBuilder.MapEnum<CampaignRole>("public.campaign_role");
            dataSourceBuilder.MapEnum<OwnershipType>("public.ownership_type");
            dataSourceBuilder.MapEnum<VisibilityLevel>("public.visibility_level");
            // Enable pgvector support on the data source builder (required for writing Vector types)
            dataSourceBuilder.UseVector();
            var dataSource = dataSourceBuilder.Build();
            
            // Register the NpgsqlDataSource as singleton for direct SQL queries (e.g., semantic search)
            services.AddSingleton(dataSource);
            
            services.AddDbContext<ApplicationDbContext>((sp, options) =>
            {
                options.UseNpgsql(
                    dataSource,
                    b => {
                        b.MigrationsAssembly(typeof(ApplicationDbContext).Assembly.FullName);
                        // Enable pgvector
                        b.UseVector();
                    });
            });

            services.AddScoped<IApplicationDbContext>(provider => 
                provider.GetRequiredService<ApplicationDbContext>());
            services.AddScoped<IUnitOfWork>(provider => 
                provider.GetRequiredService<ApplicationDbContext>());
        }
        // Note: For testing environment, DbContext registration is skipped here
        // The CustomWebApplicationFactory in Loremaster.Tests.Integration handles it

        // Repositories
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<ICampaignRepository, CampaignRepository>();
        services.AddScoped<ICampaignMemberRepository, CampaignMemberRepository>();
        services.AddScoped<ILoreEntityRepository, LoreEntityRepository>();
        services.AddScoped<IGameSystemRepository, GameSystemRepository>();
        services.AddScoped<IEntityTemplateRepository, EntityTemplateRepository>();
        services.AddScoped<IGenerationRequestRepository, GenerationRequestRepository>();
        services.AddScoped<IRagSourceRepository, RagSourceRepository>();             
        services.AddScoped<IDocumentRepository, DocumentRepository>();

        // Database seeder
        services.AddScoped<DatabaseSeeder>();

        // Services
        services.AddSingleton<IDateTimeProvider, DateTimeProvider>();
        services.AddScoped<IPasswordHasher, PasswordHasher>();
        services.AddScoped<IJwtTokenGenerator, JwtTokenGenerator>();
        services.AddScoped<IServiceTokenGenerator, ServiceTokenGenerator>();
        
        // Document processing services
        services.AddScoped<IPdfParsingService, PdfParsingService>();
        services.AddSingleton<ITextChunkingService, TextChunkingService>();
        
        // Entity generation services (RAG-assisted)
        services.AddScoped<IRagContextProvider, RagContextProvider>();
        services.AddScoped<IEntityGenerationService, EntityGenerationService>();

        // Genkit service URL based on environment
        var genkitBaseUrl = isDevelopment || isTesting
            ? configuration["GenkitService:BaseUrl"] ?? "http://localhost:3000"
            : configuration["GenkitService:ProductionUrl"] ?? configuration["GenkitService:BaseUrl"] ?? "http://genkit:3000";
        
        var genkitTimeout = int.Parse(configuration["GenkitService:TimeoutSeconds"] ?? "30");

        // Genkit AI Service with Polly retry policies
        services.AddHttpClient<IAiService, GenkitAiService>(client =>
        {
            client.BaseAddress = new Uri(genkitBaseUrl);
            client.Timeout = TimeSpan.FromSeconds(genkitTimeout);
        })
        .SetHandlerLifetime(TimeSpan.FromMinutes(5))
        .AddPolicyHandler(GetRetryPolicy())
        .AddPolicyHandler(GetCircuitBreakerPolicy());

        // Genkit Embedding Service (separate client for embeddings)
        services.AddHttpClient<IEmbeddingService, GenkitEmbeddingService>(client =>
        {
            client.BaseAddress = new Uri(genkitBaseUrl);
            client.Timeout = TimeSpan.FromSeconds(genkitTimeout * 2); // Embeddings may take longer
        })
        .SetHandlerLifetime(TimeSpan.FromMinutes(5))
        .AddPolicyHandler(GetRetryPolicy())
        .AddPolicyHandler(GetCircuitBreakerPolicy());

        // JWT Authentication
        services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        })
        .AddJwtBearer(options =>
        {
            // Disable default claim type mapping to preserve original JWT claim names
            options.MapInboundClaims = false;
            
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = configuration["Jwt:Issuer"],
                ValidAudience = configuration["Jwt:Audience"],
                IssuerSigningKey = new SymmetricSecurityKey(
                    Encoding.UTF8.GetBytes(configuration["Jwt:Secret"]!)),
                ClockSkew = TimeSpan.Zero,
                // Use simple "role" claim type to match JwtTokenGenerator
                RoleClaimType = JwtTokenGenerator.RoleClaimType
            };
        });

        // Authorization Policies
        services.AddAuthorizationBuilder()
            .AddPolicy("RequireAdminRole", policy => 
                policy.RequireRole("Admin"))
            .AddPolicy("RequireMasterRole", policy => 
                policy.RequireRole("Master", "Admin"))
            .AddPolicy("RequirePlayerRole", policy => 
                policy.RequireRole("Player", "Master", "Admin"));

        return services;
    }

    private static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy()
    {
        return HttpPolicyExtensions
            .HandleTransientHttpError()
            .OrResult(msg => msg.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
            .WaitAndRetryAsync(3, retryAttempt =>
                TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)));
    }

    private static IAsyncPolicy<HttpResponseMessage> GetCircuitBreakerPolicy()
    {
        return HttpPolicyExtensions
            .HandleTransientHttpError()
            .CircuitBreakerAsync(5, TimeSpan.FromSeconds(30));
    }
}
