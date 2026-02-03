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
    /// </summary>
    /// <param name="email">The email address for the test user.</param>
    /// <param name="password">The password for the test user.</param>
    /// <returns>A tuple containing the authenticated client, access token, and user ID.</returns>
    public async Task<(HttpClient Client, string AccessToken, string UserId)> CreateAuthenticatedClientAsync(
        string email = "test@example.com",
        string password = "TestPassword123!")
    {
        var client = _factory.CreateClient();

        // Generate display name from email prefix for easier test identification
        var displayName = email.Split('@')[0];

        // Register user as Master (Masters don't require an invitation code)
        var registerRequest = new
        {
            Email = email,
            Password = password,
            DisplayName = displayName,
            Role = "Master"
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

    private record AuthResponse(Guid UserId, string Email, string AccessToken, string RefreshToken);
}
