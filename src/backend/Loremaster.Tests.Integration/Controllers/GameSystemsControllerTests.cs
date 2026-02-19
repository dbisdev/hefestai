using System.Net;
using System.Net.Http.Json;
using Loremaster.Application.Common.Interfaces;
using Loremaster.Domain.Entities;
using Loremaster.Domain.Enums;
using Loremaster.Infrastructure.Persistence;
using Loremaster.Tests.Integration.Fixtures;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Loremaster.Tests.Integration.Controllers;

/// <summary>
/// Integration tests for GameSystemsController.
/// Tests all game system CRUD operations and access control.
/// </summary>
public class GameSystemsControllerTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public GameSystemsControllerTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    #region Helper Methods

    /// <summary>
    /// Creates a game system directly in the database for testing.
    /// </summary>
    private async Task<GameSystem> CreateGameSystemInDbAsync(
        string? code = null,
        string name = "Test Game System",
        bool isActive = true)
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        
        var ownerId = Guid.NewGuid();
        
        var gameSystem = GameSystem.Create(
            code: code ?? $"test-{Guid.NewGuid():N}".Substring(0, 20),
            name: name,
            ownerId: ownerId,
            publisher: "Test Publisher",
            version: "1.0",
            description: "A test game system"
        );
        
        if (!isActive)
        {
            gameSystem.Deactivate();
        }
        
        dbContext.GameSystems.Add(gameSystem);
        await dbContext.SaveChangesAsync();
        
        return gameSystem;
    }

    #endregion

    #region Get All Game Systems Tests

    [Fact]
    public async Task GetAll_WhenGameSystemsExist_ShouldReturnActiveOnly()
    {
        // Arrange - Create active and inactive game systems
        var activeGs = await CreateGameSystemInDbAsync(name: "Active System", isActive: true);
        await CreateGameSystemInDbAsync(name: "Inactive System", isActive: false);

        // Act - Anonymous access
        var response = await _client.GetAsync("/api/gamesystems");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var content = await response.Content.ReadFromJsonAsync<List<GameSystemResponse>>();
        content.Should().NotBeNull();
        content!.Should().Contain(gs => gs.Id == activeGs.Id);
        // Inactive systems should not be returned
        content.All(gs => gs.IsActive).Should().BeTrue();
    }

    [Fact]
    public async Task GetAll_WhenNotAuthenticated_ShouldStillWork()
    {
        // Arrange
        await CreateGameSystemInDbAsync();

        // Act - No authentication
        var response = await _client.GetAsync("/api/gamesystems");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    #endregion

    #region Get Game System By Id Tests

    [Fact]
    public async Task GetById_WhenExists_ShouldReturnGameSystem()
    {
        // Arrange
        var gameSystem = await CreateGameSystemInDbAsync(name: "Get By Id Test");

        // Act
        var response = await _client.GetAsync($"/api/gamesystems/{gameSystem.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var content = await response.Content.ReadFromJsonAsync<GameSystemResponse>();
        content.Should().NotBeNull();
        content!.Id.Should().Be(gameSystem.Id);
        content.Name.Should().Be("Get By Id Test");
    }

    [Fact]
    public async Task GetById_WhenNotExists_ShouldReturnNotFound()
    {
        // Act
        var response = await _client.GetAsync($"/api/gamesystems/{Guid.NewGuid()}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    #endregion

    #region Get Game System By Code Tests

    [Fact]
    public async Task GetByCode_WhenExists_ShouldReturnGameSystem()
    {
        // Arrange
        var code = $"bycode-{Guid.NewGuid():N}".Substring(0, 15);
        var gameSystem = await CreateGameSystemInDbAsync(code: code, name: "Get By Code Test");

        // Act
        var response = await _client.GetAsync($"/api/gamesystems/by-code/{code}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var content = await response.Content.ReadFromJsonAsync<GameSystemResponse>();
        content.Should().NotBeNull();
        content!.Code.Should().Be(code);
    }

    [Fact]
    public async Task GetByCode_WhenNotExists_ShouldReturnNotFound()
    {
        // Act
        var response = await _client.GetAsync("/api/gamesystems/by-code/nonexistent");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    #endregion

    #region Create Game System Tests (Admin Only)

    [Fact]
    public async Task Create_WhenNotAuthenticated_ShouldReturnUnauthorized()
    {
        // Arrange
        var request = new
        {
            Code = "test-code",
            Name = "Test System"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/gamesystems", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Create_WhenNotAdmin_ShouldReturnForbidden()
    {
        // Arrange - Regular user (Player, not Master or Admin)
        var authFactory = new AuthenticatedHttpClientFactory(_factory);
        var (client, _, _) = await authFactory.CreateAuthenticatedClientAsync(
            $"regular-user-{Guid.NewGuid()}@example.com", role: "Player");

        var request = new
        {
            Code = $"notadmin-{Guid.NewGuid():N}".Substring(0, 15),
            Name = "Not Admin System"
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/gamesystems", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    // Note: Admin role tests would require setting up admin users in the test fixture
    // For now, we test the authorization is enforced

    #endregion

    #region Update Game System Tests (Admin Only)

    [Fact]
    public async Task Update_WhenNotAuthenticated_ShouldReturnUnauthorized()
    {
        // Arrange
        var gameSystem = await CreateGameSystemInDbAsync();
        var request = new { Name = "Updated Name" };

        // Act
        var response = await _client.PutAsJsonAsync($"/api/gamesystems/{gameSystem.Id}", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Update_WhenNotAdmin_ShouldReturnForbidden()
    {
        // Arrange
        var gameSystem = await CreateGameSystemInDbAsync();
        
        var authFactory = new AuthenticatedHttpClientFactory(_factory);
        var (client, _, _) = await authFactory.CreateAuthenticatedClientAsync(
            $"regular-update-{Guid.NewGuid()}@example.com", role: "Player");

        var request = new { Name = "Updated Name" };

        // Act
        var response = await client.PutAsJsonAsync($"/api/gamesystems/{gameSystem.Id}", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    #endregion

    #region Update Game System Status Tests (Admin Only)

    [Fact]
    public async Task UpdateStatus_WhenNotAuthenticated_ShouldReturnUnauthorized()
    {
        // Arrange
        var gameSystem = await CreateGameSystemInDbAsync();
        var request = new { IsActive = false };

        // Act
        var response = await _client.PatchAsJsonAsync($"/api/gamesystems/{gameSystem.Id}/status", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task UpdateStatus_WhenNotAdmin_ShouldReturnForbidden()
    {
        // Arrange
        var gameSystem = await CreateGameSystemInDbAsync();
        
        var authFactory = new AuthenticatedHttpClientFactory(_factory);
        var (client, _, _) = await authFactory.CreateAuthenticatedClientAsync(
            $"regular-status-{Guid.NewGuid()}@example.com", role: "Player");

        var request = new { IsActive = false };

        // Act
        var response = await client.PatchAsJsonAsync($"/api/gamesystems/{gameSystem.Id}/status", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    #endregion

    #region Response DTOs

    private record GameSystemResponse(
        Guid Id,
        string Code,
        string Name,
        string? Publisher,
        string? Version,
        string? Description,
        List<string> SupportedEntityTypes,
        bool IsActive
    );

    #endregion
}
