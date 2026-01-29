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
    public static IServiceCollection AddInfrastructureServices(
        this IServiceCollection services, 
        IConfiguration configuration)
    {
        var environment = configuration["ASPNETCORE_ENVIRONMENT"] ?? "Production";
        var isDevelopment = environment == "Development";

        // Database
        services.AddScoped<AuditableEntityInterceptor>();
        
        // Use different connection strings based on environment
        var connectionString = isDevelopment 
            ? configuration.GetConnectionString("DefaultConnection") 
            : configuration.GetConnectionString("SupabaseConnection") 
              ?? configuration.GetConnectionString("DefaultConnection");
        
        // Configure NpgsqlDataSource with PostgreSQL enum mappings
        var dataSourceBuilder = new NpgsqlDataSourceBuilder(connectionString);
        dataSourceBuilder.MapEnum<UserRole>("user_role");
        dataSourceBuilder.MapEnum<CampaignRole>("campaign_role");
        dataSourceBuilder.MapEnum<OwnershipType>("ownership_type");
        dataSourceBuilder.MapEnum<VisibilityLevel>("visibility_level");
        var dataSource = dataSourceBuilder.Build();
        
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

        // Repositories
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<ICampaignRepository, CampaignRepository>();
        services.AddScoped<ICampaignMemberRepository, CampaignMemberRepository>();
        services.AddScoped<ILoreEntityRepository, LoreEntityRepository>();
        services.AddScoped<IGameSystemRepository, GameSystemRepository>();
        services.AddScoped<IGenerationRequestRepository, GenerationRequestRepository>();
        services.AddScoped<IRagSourceRepository, RagSourceRepository>();
        
        // Legacy repositories (to be migrated/removed)
        services.AddScoped<IProjectRepository, ProjectRepository>();
        services.AddScoped<IDocumentRepository, DocumentRepository>();

        // Services
        services.AddSingleton<IDateTimeProvider, DateTimeProvider>();
        services.AddScoped<IPasswordHasher, PasswordHasher>();
        services.AddScoped<IJwtTokenGenerator, JwtTokenGenerator>();
        services.AddScoped<IServiceTokenGenerator, ServiceTokenGenerator>();

        // Genkit service URL based on environment
        var genkitBaseUrl = isDevelopment
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
                ClockSkew = TimeSpan.Zero
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
