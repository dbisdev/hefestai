using System.Net.Http.Json;
using Loremaster.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Loremaster.Tests.Integration.Fixtures;

public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // Add test configuration for JWT
        builder.ConfigureAppConfiguration((context, config) =>
        {
            var testSettings = new Dictionary<string, string?>
            {
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
            // Remove the existing DbContext registration
            services.RemoveAll(typeof(DbContextOptions<ApplicationDbContext>));
            services.RemoveAll(typeof(ApplicationDbContext));

            // Add ApplicationDbContext using an in-memory database for testing
            // Use a unique name for each test to ensure isolation
            services.AddDbContext<ApplicationDbContext>(options =>
            {
                options.UseInMemoryDatabase($"InMemoryDbForTesting-{Guid.NewGuid()}");
            });

            // Build the service provider and ensure database is created
            var sp = services.BuildServiceProvider();
            using var scope = sp.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            db.Database.EnsureCreated();
        });

        builder.UseEnvironment("Testing");
    }
}

public class AuthenticatedHttpClientFactory
{
    private readonly CustomWebApplicationFactory _factory;

    public AuthenticatedHttpClientFactory(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    public async Task<(HttpClient Client, string AccessToken, string UserId)> CreateAuthenticatedClientAsync(
        string email = "test@example.com",
        string password = "TestPassword123!")
    {
        var client = _factory.CreateClient();

        // Register user
        var registerRequest = new
        {
            Email = email,
            Password = password,
            DisplayName = "Test User"
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
