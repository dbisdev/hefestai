using System.Net;
using System.Net.Http.Json;
using Loremaster.Domain.Entities;
using Loremaster.Domain.Enums;
using Loremaster.Infrastructure.Persistence;
using Loremaster.Tests.Integration.Fixtures;
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
    /// Creates a game system directly in the database owned by the given user.
    /// </summary>
    private async Task<GameSystem> CreateGameSystemInDbAsync(
        Guid ownerId,
        string? code = null,
        string name = "Test Game System",
        bool isActive = true)
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var gameSystem = GameSystem.Create(
            code: code ?? $"test-{Guid.NewGuid():N}".Substring(0, 20),
            name: name,
            ownerId: ownerId,
            publisher: "Test Publisher",
            version: "1.0",
            description: "A test game system"
        );

        if (!isActive)
            gameSystem.Deactivate();

        dbContext.GameSystems.Add(gameSystem);
        await dbContext.SaveChangesAsync();

        return gameSystem;
    }

    /// <summary>
    /// Registers a user with the given role and returns an authenticated client + userId.
    /// </summary>
    private async Task<(HttpClient Client, Guid UserId)> CreateAuthenticatedUserAsync(string role)
    {
        var authFactory = new AuthenticatedHttpClientFactory(_factory);
        var email = $"{role.ToLower()}-{Guid.NewGuid()}@test.com";
        var (client, _, userId) = await authFactory.CreateAuthenticatedClientAsync(email, role: role);
        return (client, Guid.Parse(userId));
    }

    /// <summary>
    /// Creates a campaign directly in the database owned by the given user and using the given game system.
    /// </summary>
    private async Task<Campaign> CreateCampaignInDbAsync(Guid ownerId, Guid gameSystemId, string name = "Test Campaign")
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var campaign = Campaign.Create(
            ownerId: ownerId,
            gameSystemId: gameSystemId,
            name: name,
            description: "Integration test campaign"
        );

        dbContext.Campaigns.Add(campaign);
        await dbContext.SaveChangesAsync();
        return campaign;
    }

    /// <summary>
    /// Creates a campaign membership directly in the database.
    /// </summary>
    private async Task CreateCampaignMemberInDbAsync(Guid campaignId, Guid userId, CampaignRole role)
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var member = CampaignMember.Create(campaignId, userId, role);
        dbContext.CampaignMembers.Add(member);
        await dbContext.SaveChangesAsync();
    }

    #endregion

    #region Get All Game Systems Tests

    [Fact]
    public async Task GetAll_WhenNotAuthenticated_ShouldReturnUnauthorized()
    {
        // Act - No authentication token
        var response = await _client.GetAsync("/api/gamesystems");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetAll_WhenPlayer_NotInAnyCampaign_ShouldReturnOnlyAdminOwnedSystems()
    {
        // Arrange — Player not a member of any campaign; should only see Admin-owned systems.
        var (playerClient, _) = await CreateAuthenticatedUserAsync("Player");
        var (_, adminId)      = await CreateAuthenticatedUserAsync("Admin");
        var (_, masterId)     = await CreateAuthenticatedUserAsync("Master");

        var adminSystem  = await CreateGameSystemInDbAsync(adminId,  name: "Admin System for Player Test");
        var masterSystem = await CreateGameSystemInDbAsync(masterId, name: "Master System Player Should Not See");

        // Act
        var response = await playerClient.GetAsync("/api/gamesystems");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var content = await response.Content.ReadFromJsonAsync<List<GameSystemResponse>>();
        content.Should().NotBeNull();
        content!.Should().Contain(gs => gs.Id == adminSystem.Id,  "player sees Admin-owned systems");
        content!.Should().NotContain(gs => gs.Id == masterSystem.Id, "player must not see Master-owned system without campaign link");
    }

    [Fact]
    public async Task GetAll_WhenPlayer_InCampaignWithMaster_ShouldSeeMastersSystem()
    {
        // Arrange — Player is a member of a campaign whose Master owns a game system.
        var (playerClient, playerId) = await CreateAuthenticatedUserAsync("Player");
        var (_, masterId)            = await CreateAuthenticatedUserAsync("Master");
        var (_, adminId)             = await CreateAuthenticatedUserAsync("Admin");
        var (_, otherMasterId)       = await CreateAuthenticatedUserAsync("Master");

        // Campaign owned by the Master (game system id is required by the domain — use a throwaway).
        var campaignGameSystem  = await CreateGameSystemInDbAsync(adminId, name: "Campaign GameSystem Admin");
        var masterOwnedSystem   = await CreateGameSystemInDbAsync(masterId, name: "Master Owned System for Player Campaign Test");
        var otherMasterSystem   = await CreateGameSystemInDbAsync(otherMasterId, name: "Other Master System Player Should Not See");
        var inactiveSystem      = await CreateGameSystemInDbAsync(masterId, name: "Inactive Master System", isActive: false);

        var campaign = await CreateCampaignInDbAsync(masterId, campaignGameSystem.Id, "Player Campaign Test");
        await CreateCampaignMemberInDbAsync(campaign.Id, masterId, CampaignRole.Master);
        await CreateCampaignMemberInDbAsync(campaign.Id, playerId, CampaignRole.Player);

        // Act
        var response = await playerClient.GetAsync("/api/gamesystems");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var content = await response.Content.ReadFromJsonAsync<List<GameSystemResponse>>();
        content.Should().NotBeNull();

        content!.Should().Contain(gs => gs.Id == campaignGameSystem.Id, "player sees admin-owned system used by campaign");
        content!.Should().Contain(gs => gs.Id == masterOwnedSystem.Id,  "player sees system owned by their campaign's Master");
        content!.Should().NotContain(gs => gs.Id == otherMasterSystem.Id, "player must not see system from unrelated Master");
        content!.Should().NotContain(gs => gs.Id == inactiveSystem.Id,   "inactive systems must not appear");
        content!.All(gs => gs.IsActive).Should().BeTrue();
    }

    [Fact]
    public async Task GetAll_WhenMaster_ShouldReturnOnlyActiveSystemsOwnedByAdminOrSelf()
    {
        // Arrange — create two Masters; each owns one system; an Admin-owned system also exists
        var (masterClient, masterId) = await CreateAuthenticatedUserAsync("Master");
        var (_, otherMasterId) = await CreateAuthenticatedUserAsync("Master");
        var (_, adminId) = await CreateAuthenticatedUserAsync("Admin");

        var ownSystem   = await CreateGameSystemInDbAsync(masterId,      name: "Master Own System");
        var adminSystem = await CreateGameSystemInDbAsync(adminId,        name: "Admin Public System");
        var otherSystem = await CreateGameSystemInDbAsync(otherMasterId,  name: "Other Master System");
        var inactiveGs  = await CreateGameSystemInDbAsync(masterId,       name: "Inactive System", isActive: false);

        // Act
        var response = await masterClient.GetAsync("/api/gamesystems");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var content = await response.Content.ReadFromJsonAsync<List<GameSystemResponse>>();
        content.Should().NotBeNull();

        // Master sees own system and Admin-owned system
        content!.Should().Contain(gs => gs.Id == ownSystem.Id,   "master sees own system");
        content!.Should().Contain(gs => gs.Id == adminSystem.Id,  "master sees admin-owned system");

        // Master does NOT see other master's system or inactive systems
        content!.Should().NotContain(gs => gs.Id == otherSystem.Id,  "master must not see other master's system");
        content!.Should().NotContain(gs => gs.Id == inactiveGs.Id,   "inactive systems must not appear");
        content!.All(gs => gs.IsActive).Should().BeTrue();
    }

    [Fact]
    public async Task GetAll_WhenAdmin_ShouldReturnAllActiveSystems()
    {
        // Arrange
        var (adminClient, adminId) = await CreateAuthenticatedUserAsync("Admin");
        var (_, masterId)          = await CreateAuthenticatedUserAsync("Master");

        var adminSystem  = await CreateGameSystemInDbAsync(adminId,  name: "Admin System for Admin Test");
        var masterSystem = await CreateGameSystemInDbAsync(masterId, name: "Master System for Admin Test");
        var inactiveGs   = await CreateGameSystemInDbAsync(adminId,  name: "Inactive for Admin Test", isActive: false);

        // Act
        var response = await adminClient.GetAsync("/api/gamesystems");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var content = await response.Content.ReadFromJsonAsync<List<GameSystemResponse>>();
        content.Should().NotBeNull();

        // Admin sees systems from both owners
        content!.Should().Contain(gs => gs.Id == adminSystem.Id,  "admin sees own system");
        content!.Should().Contain(gs => gs.Id == masterSystem.Id, "admin sees master's system");

        // No inactive systems
        content!.Should().NotContain(gs => gs.Id == inactiveGs.Id);
        content!.All(gs => gs.IsActive).Should().BeTrue();
    }

    #endregion

    #region Get Game System By Id Tests

    [Fact]
    public async Task GetById_WhenNotAuthenticated_ShouldReturnUnauthorized()
    {
        // Act
        var response = await _client.GetAsync($"/api/gamesystems/{Guid.NewGuid()}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetById_WhenAuthenticated_AndExists_ShouldReturnGameSystem()
    {
        // Arrange
        var (_, ownerId) = await CreateAuthenticatedUserAsync("Master");
        var (masterClient, _) = await CreateAuthenticatedUserAsync("Master");
        var gameSystem = await CreateGameSystemInDbAsync(ownerId, name: "Get By Id Test");

        // Act
        var response = await masterClient.GetAsync($"/api/gamesystems/{gameSystem.Id}");

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
        // Arrange
        var (masterClient, _) = await CreateAuthenticatedUserAsync("Master");

        // Act
        var response = await masterClient.GetAsync($"/api/gamesystems/{Guid.NewGuid()}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    #endregion

    #region Get Game System By Code Tests

    [Fact]
    public async Task GetByCode_WhenNotAuthenticated_ShouldReturnUnauthorized()
    {
        // Act
        var response = await _client.GetAsync("/api/gamesystems/by-code/anycode");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetByCode_WhenAuthenticated_AndExists_ShouldReturnGameSystem()
    {
        // Arrange
        var (_, ownerId) = await CreateAuthenticatedUserAsync("Master");
        var (masterClient, _) = await CreateAuthenticatedUserAsync("Master");
        var code = $"bycode-{Guid.NewGuid():N}".Substring(0, 15);
        await CreateGameSystemInDbAsync(ownerId, code: code, name: "Get By Code Test");

        // Act
        var response = await masterClient.GetAsync($"/api/gamesystems/by-code/{code}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var content = await response.Content.ReadFromJsonAsync<GameSystemResponse>();
        content.Should().NotBeNull();
        content!.Code.Should().Be(code);
    }

    [Fact]
    public async Task GetByCode_WhenNotExists_ShouldReturnNotFound()
    {
        // Arrange
        var (masterClient, _) = await CreateAuthenticatedUserAsync("Master");

        // Act
        var response = await masterClient.GetAsync("/api/gamesystems/by-code/nonexistent");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    #endregion

    #region Create Game System Tests (Master/Admin only)

    [Fact]
    public async Task Create_WhenNotAuthenticated_ShouldReturnUnauthorized()
    {
        // Arrange
        var request = new { Code = "test-code", Name = "Test System" };

        // Act
        var response = await _client.PostAsJsonAsync("/api/gamesystems", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Create_WhenPlayer_ShouldReturnForbidden()
    {
        // Arrange
        var (client, _) = await CreateAuthenticatedUserAsync("Player");
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

    #endregion

    #region Update Game System Tests (Master/Admin only)

    [Fact]
    public async Task Update_WhenNotAuthenticated_ShouldReturnUnauthorized()
    {
        // Arrange
        var (_, ownerId) = await CreateAuthenticatedUserAsync("Master");
        var gameSystem = await CreateGameSystemInDbAsync(ownerId);
        var request = new { Name = "Updated Name" };

        // Act
        var response = await _client.PutAsJsonAsync($"/api/gamesystems/{gameSystem.Id}", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Update_WhenPlayer_ShouldReturnForbidden()
    {
        // Arrange
        var (_, ownerId) = await CreateAuthenticatedUserAsync("Master");
        var gameSystem = await CreateGameSystemInDbAsync(ownerId);
        var (playerClient, _) = await CreateAuthenticatedUserAsync("Player");
        var request = new { Name = "Updated Name" };

        // Act
        var response = await playerClient.PutAsJsonAsync($"/api/gamesystems/{gameSystem.Id}", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    #endregion

    #region Update Game System Status Tests (Master/Admin only)

    [Fact]
    public async Task UpdateStatus_WhenNotAuthenticated_ShouldReturnUnauthorized()
    {
        // Arrange
        var (_, ownerId) = await CreateAuthenticatedUserAsync("Master");
        var gameSystem = await CreateGameSystemInDbAsync(ownerId);
        var request = new { IsActive = false };

        // Act
        var response = await _client.PatchAsJsonAsync($"/api/gamesystems/{gameSystem.Id}/status", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task UpdateStatus_WhenPlayer_ShouldReturnForbidden()
    {
        // Arrange
        var (_, ownerId) = await CreateAuthenticatedUserAsync("Master");
        var gameSystem = await CreateGameSystemInDbAsync(ownerId);
        var (playerClient, _) = await CreateAuthenticatedUserAsync("Player");
        var request = new { IsActive = false };

        // Act
        var response = await playerClient.PatchAsJsonAsync($"/api/gamesystems/{gameSystem.Id}/status", request);

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
