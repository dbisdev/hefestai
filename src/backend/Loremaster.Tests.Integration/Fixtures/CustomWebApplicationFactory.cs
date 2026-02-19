using System.Net.Http.Json;
using Loremaster.Application.Common.Interfaces;
using Loremaster.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Npgsql;

namespace Loremaster.Tests.Integration.Fixtures;

/// <summary>
/// Custom web application factory for integration tests.
/// Uses InMemory database instead of PostgreSQL.
/// </summary>
public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // Set testing environment FIRST before configuration runs
        builder.UseEnvironment("Testing");
        
        // Add test configuration
        builder.ConfigureAppConfiguration((context, config) =>
        {
            var testSettings = new Dictionary<string, string?>
            {
                ["ASPNETCORE_ENVIRONMENT"] = "Testing",
                ["Jwt:Secret"] = "ThisIsATestSecretKeyThatIsAtLeast32CharactersLong!",
                ["Jwt:Issuer"] = "LoremasterTest",
                ["Jwt:Audience"] = "LoremasterTestAudience",
                ["Jwt:AccessTokenExpirationMinutes"] = "60",
                ["Jwt:RefreshTokenExpirationDays"] = "7",
                ["RateLimiting:EnableRateLimiting"] = "false",
                ["GenkitService:BaseUrl"] = "http://localhost:3001",
                ["GenkitService:ServiceSecret"] = "test-service-secret-key"
            };

            config.AddInMemoryCollection(testSettings);
        });

        builder.ConfigureServices(services =>
        {
            // Remove the existing DbContext and related registrations
            services.RemoveAll(typeof(DbContextOptions<ApplicationDbContext>));
            services.RemoveAll(typeof(ApplicationDbContext));
            services.RemoveAll(typeof(IApplicationDbContext));
            services.RemoveAll(typeof(IUnitOfWork));
            services.RemoveAll(typeof(NpgsqlDataSource));
            
            // Generate a unique database name for test isolation
            var dbName = $"InMemoryDbForTesting-{Guid.NewGuid()}";

            // Add ApplicationDbContext using an in-memory database for testing
            services.AddDbContext<ApplicationDbContext>((sp, options) =>
            {
                options.UseInMemoryDatabase(dbName);
                
                // Ignore warnings about properties not being mapped
                // InMemory provider doesn't support JsonDocument, but we suppress the validation
                options.ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning));
            });
            
            // Re-register the interfaces
            services.AddScoped<IApplicationDbContext>(provider => 
                provider.GetRequiredService<ApplicationDbContext>());
            services.AddScoped<IUnitOfWork>(provider => 
                provider.GetRequiredService<ApplicationDbContext>());

            // Build the service provider and ensure database is created
            var sp = services.BuildServiceProvider();
            using var scope = sp.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            db.Database.EnsureCreated();
        });
    }
}

/// <summary>
/// Factory for creating authenticated HTTP clients for integration tests.
/// </summary>
public class AuthenticatedHttpClientFactory
{
    private readonly CustomWebApplicationFactory _factory;

    public AuthenticatedHttpClientFactory(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    /// <summary>
    /// Creates an authenticated HTTP client by registering a new user or logging in an existing one.
    /// Players can now register without a campaign code, but optionally can join one during registration.
    /// </summary>
    /// <param name="email">The email address for the test user.</param>
    /// <param name="password">The password for the test user.</param>
    /// <param name="role">The role for the test user (default: Master).</param>
    /// <param name="joinCode">Optional campaign join code to join during registration.</param>
    /// <returns>A tuple containing the authenticated client, access token, and user ID.</returns>
    public async Task<(HttpClient Client, string AccessToken, string UserId)> CreateAuthenticatedClientAsync(
        string email = "test@example.com",
        string password = "TestPassword123!",
        string role = "Master",
        string? joinCode = null)
    {
        var client = _factory.CreateClient();

        // Generate display name from email prefix for easier test identification
        var displayName = email.Split('@')[0];

        // Build the register request - inviteCode is now optional for joining a campaign
        var registerRequest = new
        {
            Email = email,
            Password = password,
            DisplayName = displayName,
            Role = role,
            InviteCode = joinCode
        };
        
        var registerResponse = await client.PostAsJsonAsync("/api/auth/register", registerRequest);

        if (!registerResponse.IsSuccessStatusCode)
        {
            var errorContent = await registerResponse.Content.ReadAsStringAsync();
            
            // User might already exist, try login
            var loginResponse = await client.PostAsJsonAsync("/api/auth/login", new
            {
                Email = email,
                Password = password
            });
            
            if (!loginResponse.IsSuccessStatusCode)
            {
                var loginError = await loginResponse.Content.ReadAsStringAsync();
                throw new Exception($"Failed to register or login.\nRegister error: {errorContent}\nLogin error: {loginError}");
            }
            
            var loginResult = await loginResponse.Content.ReadFromJsonAsync<AuthResponse>();
            client.DefaultRequestHeaders.Authorization = 
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", loginResult!.AccessToken);
            return (client, loginResult.AccessToken, loginResult.UserId.ToString());
        }

        var result = await registerResponse.Content.ReadFromJsonAsync<AuthResponse>();
        client.DefaultRequestHeaders.Authorization = 
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", result!.AccessToken);
        
        return (client, result.AccessToken, result.UserId.ToString());
    }

    /// <summary>
    /// Creates a Master user with a campaign and returns the campaign's join code.
    /// Use this when you need to test Player joining functionality.
    /// </summary>
    public async Task<string> CreateMasterWithCampaignAsync(HttpClient client, string campaignName = "Test Campaign")
    {
        var masterEmail = $"master-{Guid.NewGuid()}@test.com";
        var masterPassword = "MasterPassword123!";
        
        // Register Master
        var registerRequest = new
        {
            Email = masterEmail,
            Password = masterPassword,
            DisplayName = "TestMaster",
            Role = "Master"
        };
        
        var registerResponse = await client.PostAsJsonAsync("/api/auth/register", registerRequest);
        registerResponse.EnsureSuccessStatusCode();
        
        var registerResult = await registerResponse.Content.ReadFromJsonAsync<AuthResponse>();
        var masterToken = registerResult!.AccessToken;
        
        // Set authorization for campaign creation
        client.DefaultRequestHeaders.Authorization = 
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", masterToken);
        
        // Create a game system first
        var gameSystemResponse = await client.PostAsJsonAsync("/api/game-systems", new
        {
            Name = "Test RPG System",
            Description = "A test game system"
        });
        gameSystemResponse.EnsureSuccessStatusCode();
        var gameSystem = await gameSystemResponse.Content.ReadFromJsonAsync<GameSystemResponse>();
        
        // Create a campaign
        var campaignResponse = await client.PostAsJsonAsync("/api/campaigns", new
        {
            Name = campaignName,
            Description = "A test campaign",
            GameSystemId = gameSystem!.Id
        });
        campaignResponse.EnsureSuccessStatusCode();
        var campaign = await campaignResponse.Content.ReadFromJsonAsync<CampaignResponse>();
        
        // Clear authorization
        client.DefaultRequestHeaders.Authorization = null;
        
        return campaign!.JoinCode;
    }

    private record AuthResponse(Guid UserId, string Email, string AccessToken, string RefreshToken);
    private record CampaignResponse(Guid Id, string Name, string JoinCode);
    private record GameSystemResponse(Guid Id, string Name);
}
