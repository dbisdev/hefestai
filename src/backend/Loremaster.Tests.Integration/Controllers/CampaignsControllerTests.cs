using System.Net;
using System.Net.Http.Json;
using Loremaster.Domain.Entities;
using Loremaster.Domain.Enums;
using Loremaster.Infrastructure.Persistence;
using Loremaster.Tests.Integration.Fixtures;
using Microsoft.Extensions.DependencyInjection;

namespace Loremaster.Tests.Integration.Controllers;

/// <summary>
/// Integration tests for CampaignsController.
/// Tests all campaign CRUD operations, membership management, and access control.
/// </summary>
public class CampaignsControllerTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public CampaignsControllerTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    #region Helper Methods

    /// <summary>
    /// Creates a game system in the test database and returns its ID.
    /// </summary>
    private async Task<Guid> CreateGameSystemAsync(HttpClient client)
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        
        var gameSystem = GameSystem.Create(
            code: $"test-{Guid.NewGuid():N}".Substring(0, 20),
            name: "Test Game System",
            ownerId: Guid.NewGuid(),
            publisher: "Test Publisher",
            version: "1.0",
            description: "A test game system for integration tests"
        );
        
        dbContext.GameSystems.Add(gameSystem);
        await dbContext.SaveChangesAsync();
        
        return gameSystem.Id;
    }

    /// <summary>
    /// Creates an authenticated client and a game system, returning both.
    /// </summary>
    private async Task<(HttpClient Client, string UserId, Guid GameSystemId)> CreateAuthenticatedClientWithGameSystemAsync(
        string? email = null)
    {
        var authFactory = new AuthenticatedHttpClientFactory(_factory);
        var (client, _, userId) = await authFactory.CreateAuthenticatedClientAsync(
            email ?? $"campaign-user-{Guid.NewGuid()}@example.com");
        
        var gameSystemId = await CreateGameSystemAsync(client);
        
        return (client, userId, gameSystemId);
    }

    /// <summary>
    /// Helper to create a campaign and return its response.
    /// </summary>
    private async Task<CampaignDetailResponse> CreateCampaignAsync(
        HttpClient client,
        Guid gameSystemId,
        string name = "Test Campaign",
        string? description = null)
    {
        var request = new
        {
            Name = name,
            GameSystemId = gameSystemId,
            Description = description ?? "A test campaign"
        };

        var response = await client.PostAsJsonAsync("/api/campaigns", request);
        response.EnsureSuccessStatusCode();
        
        return (await response.Content.ReadFromJsonAsync<CampaignDetailResponse>())!;
    }

    #endregion

    #region Create Campaign Tests

    [Fact]
    public async Task CreateCampaign_WhenAuthenticated_ShouldReturnCreated()
    {
        // Arrange
        var (client, userId, gameSystemId) = await CreateAuthenticatedClientWithGameSystemAsync();

        var request = new
        {
            Name = "My New Campaign",
            GameSystemId = gameSystemId,
            Description = "An epic adventure awaits"
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/campaigns", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        
        var content = await response.Content.ReadFromJsonAsync<CampaignDetailResponse>();
        content.Should().NotBeNull();
        content!.Name.Should().Be(request.Name);
        content.Description.Should().Be(request.Description);
        content.GameSystemId.Should().Be(gameSystemId);
        content.JoinCode.Should().NotBeNullOrEmpty();
        content.JoinCode.Should().HaveLength(8);
        content.UserRole.Should().Be(CampaignRole.Master);
        content.MemberCount.Should().Be(1);
        content.IsActive.Should().BeTrue();
        content.OwnerId.ToString().Should().Be(userId);
    }

    [Fact]
    public async Task CreateCampaign_WhenNotAuthenticated_ShouldReturnUnauthorized()
    {
        // Arrange
        var request = new
        {
            Name = "Test Campaign",
            GameSystemId = Guid.NewGuid(),
            Description = "Description"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/campaigns", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task CreateCampaign_WithInvalidGameSystem_ShouldReturnBadRequest()
    {
        // Arrange
        var authFactory = new AuthenticatedHttpClientFactory(_factory);
        var (client, _, _) = await authFactory.CreateAuthenticatedClientAsync(
            $"invalid-gs-{Guid.NewGuid()}@example.com");

        var request = new
        {
            Name = "Test Campaign",
            GameSystemId = Guid.NewGuid(), // Non-existent game system
            Description = "Description"
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/campaigns", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Theory]
    [InlineData("", "Description")]
    [InlineData(null, "Description")]
    public async Task CreateCampaign_WithInvalidName_ShouldReturnBadRequest(string? name, string description)
    {
        // Arrange
        var (client, _, gameSystemId) = await CreateAuthenticatedClientWithGameSystemAsync(
            $"invalid-name-{Guid.NewGuid()}@example.com");

        var request = new
        {
            Name = name,
            GameSystemId = gameSystemId,
            Description = description
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/campaigns", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    #endregion

    #region Get My Campaigns Tests

    [Fact]
    public async Task GetMyCampaigns_WhenAuthenticated_ShouldReturnOk()
    {
        // Arrange
        var (client, _, gameSystemId) = await CreateAuthenticatedClientWithGameSystemAsync(
            $"get-campaigns-{Guid.NewGuid()}@example.com");

        // Create some campaigns
        await CreateCampaignAsync(client, gameSystemId, "Campaign 1");
        await CreateCampaignAsync(client, gameSystemId, "Campaign 2");

        // Act
        var response = await client.GetAsync("/api/campaigns");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var content = await response.Content.ReadFromJsonAsync<List<CampaignResponse>>();
        content.Should().NotBeNull();
        content.Should().HaveCount(2);
        content!.All(c => c.UserRole == CampaignRole.Master).Should().BeTrue();
    }

    [Fact]
    public async Task GetMyCampaigns_WhenNotAuthenticated_ShouldReturnUnauthorized()
    {
        // Act
        var response = await _client.GetAsync("/api/campaigns");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    #endregion

    #region Get Campaign By Id Tests

    [Fact]
    public async Task GetCampaignById_WhenMember_ShouldReturnOk()
    {
        // Arrange
        var (client, _, gameSystemId) = await CreateAuthenticatedClientWithGameSystemAsync(
            $"get-by-id-{Guid.NewGuid()}@example.com");

        var campaign = await CreateCampaignAsync(client, gameSystemId, "Get By Id Campaign");

        // Act
        var response = await client.GetAsync($"/api/campaigns/{campaign.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var content = await response.Content.ReadFromJsonAsync<CampaignDetailResponse>();
        content.Should().NotBeNull();
        content!.Id.Should().Be(campaign.Id);
        content.Name.Should().Be("Get By Id Campaign");
        content.JoinCode.Should().NotBeNullOrEmpty(); // Master can see join code
    }

    [Fact]
    public async Task GetCampaignById_WhenNotMember_ShouldReturnForbidden()
    {
        // Arrange - User 1 creates campaign
        var (client1, _, gameSystemId) = await CreateAuthenticatedClientWithGameSystemAsync(
            $"owner-{Guid.NewGuid()}@example.com");
        var campaign = await CreateCampaignAsync(client1, gameSystemId);

        // User 2 tries to access
        var authFactory = new AuthenticatedHttpClientFactory(_factory);
        var (client2, _, _) = await authFactory.CreateAuthenticatedClientAsync(
            $"other-{Guid.NewGuid()}@example.com");

        // Act
        var response = await client2.GetAsync($"/api/campaigns/{campaign.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task GetCampaignById_WithNonExistentId_ShouldReturnNotFound()
    {
        // Arrange
        var authFactory = new AuthenticatedHttpClientFactory(_factory);
        var (client, _, _) = await authFactory.CreateAuthenticatedClientAsync(
            $"notfound-{Guid.NewGuid()}@example.com");

        // Act
        var response = await client.GetAsync($"/api/campaigns/{Guid.NewGuid()}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    #endregion

    #region Update Campaign Tests

    [Fact]
    public async Task UpdateCampaign_WhenMaster_ShouldReturnOk()
    {
        // Arrange
        var (client, _, gameSystemId) = await CreateAuthenticatedClientWithGameSystemAsync(
            $"update-{Guid.NewGuid()}@example.com");

        var campaign = await CreateCampaignAsync(client, gameSystemId, "Original Name");

        var updateRequest = new
        {
            Name = "Updated Name",
            Description = "Updated Description"
        };

        // Act
        var response = await client.PutAsJsonAsync($"/api/campaigns/{campaign.Id}", updateRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var content = await response.Content.ReadFromJsonAsync<CampaignDetailResponse>();
        content!.Name.Should().Be("Updated Name");
        content.Description.Should().Be("Updated Description");
    }

    [Fact]
    public async Task UpdateCampaign_WhenNotMaster_ShouldReturnForbidden()
    {
        // Arrange - Master creates campaign
        var (masterClient, _, gameSystemId) = await CreateAuthenticatedClientWithGameSystemAsync(
            $"master-{Guid.NewGuid()}@example.com");
        var campaign = await CreateCampaignAsync(masterClient, gameSystemId);

        // Player joins
        var authFactory = new AuthenticatedHttpClientFactory(_factory);
        var (playerClient, _, _) = await authFactory.CreateAuthenticatedClientAsync(
            $"player-update-{Guid.NewGuid()}@example.com");
        await playerClient.PostAsJsonAsync("/api/campaigns/join", new { JoinCode = campaign.JoinCode });

        // Player tries to update
        var updateRequest = new { Name = "Hacked Name", Description = "Hacked" };

        // Act
        var response = await playerClient.PutAsJsonAsync($"/api/campaigns/{campaign.Id}", updateRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    #endregion

    #region Delete Campaign Tests

    [Fact]
    public async Task DeleteCampaign_WhenOwner_ShouldReturnNoContent()
    {
        // Arrange
        var (client, _, gameSystemId) = await CreateAuthenticatedClientWithGameSystemAsync(
            $"delete-{Guid.NewGuid()}@example.com");

        var campaign = await CreateCampaignAsync(client, gameSystemId, "To Delete");

        // Act
        var response = await client.DeleteAsync($"/api/campaigns/{campaign.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Verify it's deleted
        var getResponse = await client.GetAsync($"/api/campaigns/{campaign.Id}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DeleteCampaign_WhenNotOwner_ShouldReturnForbidden()
    {
        // Arrange - Owner creates campaign
        var (ownerClient, _, gameSystemId) = await CreateAuthenticatedClientWithGameSystemAsync(
            $"owner-delete-{Guid.NewGuid()}@example.com");
        var campaign = await CreateCampaignAsync(ownerClient, gameSystemId);

        // Other user tries to delete (not even a member)
        var authFactory = new AuthenticatedHttpClientFactory(_factory);
        var (otherClient, _, _) = await authFactory.CreateAuthenticatedClientAsync(
            $"other-delete-{Guid.NewGuid()}@example.com");

        // Act
        var response = await otherClient.DeleteAsync($"/api/campaigns/{campaign.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    #endregion

    #region Join Campaign Tests

    [Fact]
    public async Task JoinCampaign_WithValidCode_ShouldReturnOk()
    {
        // Arrange - Master creates campaign
        var (masterClient, _, gameSystemId) = await CreateAuthenticatedClientWithGameSystemAsync(
            $"master-join-{Guid.NewGuid()}@example.com");
        var campaign = await CreateCampaignAsync(masterClient, gameSystemId);

        // Player joins
        var authFactory = new AuthenticatedHttpClientFactory(_factory);
        var (playerClient, _, _) = await authFactory.CreateAuthenticatedClientAsync(
            $"player-join-{Guid.NewGuid()}@example.com");

        // Act
        var response = await playerClient.PostAsJsonAsync("/api/campaigns/join", new { JoinCode = campaign.JoinCode });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var content = await response.Content.ReadFromJsonAsync<CampaignResponse>();
        content.Should().NotBeNull();
        content!.Id.Should().Be(campaign.Id);
        content.UserRole.Should().Be(CampaignRole.Player);
    }

    [Fact]
    public async Task JoinCampaign_WithInvalidCode_ShouldReturnNotFound()
    {
        // Arrange
        var authFactory = new AuthenticatedHttpClientFactory(_factory);
        var (client, _, _) = await authFactory.CreateAuthenticatedClientAsync(
            $"invalid-code-{Guid.NewGuid()}@example.com");

        // Act
        var response = await client.PostAsJsonAsync("/api/campaigns/join", new { JoinCode = "INVALID1" });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task JoinCampaign_WhenAlreadyMember_ShouldReturnBadRequest()
    {
        // Arrange
        var (client, _, gameSystemId) = await CreateAuthenticatedClientWithGameSystemAsync(
            $"already-member-{Guid.NewGuid()}@example.com");
        var campaign = await CreateCampaignAsync(client, gameSystemId);

        // Act - Owner tries to join again
        var response = await client.PostAsJsonAsync("/api/campaigns/join", new { JoinCode = campaign.JoinCode });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task JoinCampaign_WhenCampaignInactive_ShouldReturnBadRequest()
    {
        // Arrange - Master creates campaign and deactivates it
        var (masterClient, _, gameSystemId) = await CreateAuthenticatedClientWithGameSystemAsync(
            $"inactive-master-{Guid.NewGuid()}@example.com");
        var campaign = await CreateCampaignAsync(masterClient, gameSystemId);
        
        // Deactivate campaign
        await masterClient.PatchAsJsonAsync($"/api/campaigns/{campaign.Id}/status", new { IsActive = false });

        // Player tries to join
        var authFactory = new AuthenticatedHttpClientFactory(_factory);
        var (playerClient, _, _) = await authFactory.CreateAuthenticatedClientAsync(
            $"player-inactive-{Guid.NewGuid()}@example.com");

        // Act
        var response = await playerClient.PostAsJsonAsync("/api/campaigns/join", new { JoinCode = campaign.JoinCode });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    #endregion

    #region Leave Campaign Tests

    [Fact]
    public async Task LeaveCampaign_WhenPlayer_ShouldReturnNoContent()
    {
        // Arrange - Master creates campaign
        var (masterClient, _, gameSystemId) = await CreateAuthenticatedClientWithGameSystemAsync(
            $"master-leave-{Guid.NewGuid()}@example.com");
        var campaign = await CreateCampaignAsync(masterClient, gameSystemId);

        // Player joins
        var authFactory = new AuthenticatedHttpClientFactory(_factory);
        var (playerClient, _, _) = await authFactory.CreateAuthenticatedClientAsync(
            $"player-leave-{Guid.NewGuid()}@example.com");
        await playerClient.PostAsJsonAsync("/api/campaigns/join", new { JoinCode = campaign.JoinCode });

        // Act - Player leaves
        var response = await playerClient.PostAsync($"/api/campaigns/{campaign.Id}/leave", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Verify player can no longer access campaign
        var getResponse = await playerClient.GetAsync($"/api/campaigns/{campaign.Id}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task LeaveCampaign_WhenOwner_ShouldReturnBadRequest()
    {
        // Arrange
        var (client, _, gameSystemId) = await CreateAuthenticatedClientWithGameSystemAsync(
            $"owner-leave-{Guid.NewGuid()}@example.com");
        var campaign = await CreateCampaignAsync(client, gameSystemId);

        // Act - Owner tries to leave
        var response = await client.PostAsync($"/api/campaigns/{campaign.Id}/leave", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    #endregion

    #region Get Campaign Members Tests

    [Fact]
    public async Task GetCampaignMembers_WhenMember_ShouldReturnOk()
    {
        // Arrange - Master creates campaign
        var (masterClient, _, gameSystemId) = await CreateAuthenticatedClientWithGameSystemAsync(
            $"master-members-{Guid.NewGuid()}@example.com");
        var campaign = await CreateCampaignAsync(masterClient, gameSystemId);

        // Player joins
        var authFactory = new AuthenticatedHttpClientFactory(_factory);
        var (playerClient, _, _) = await authFactory.CreateAuthenticatedClientAsync(
            $"player-members-{Guid.NewGuid()}@example.com");
        await playerClient.PostAsJsonAsync("/api/campaigns/join", new { JoinCode = campaign.JoinCode });

        // Act - Get members
        var response = await masterClient.GetAsync($"/api/campaigns/{campaign.Id}/members");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var content = await response.Content.ReadFromJsonAsync<List<CampaignMemberResponse>>();
        content.Should().NotBeNull();
        content.Should().HaveCount(2);
        content!.Count(m => m.Role == CampaignRole.Master).Should().Be(1);
        content!.Count(m => m.Role == CampaignRole.Player).Should().Be(1);
    }

    [Fact]
    public async Task GetCampaignMembers_WhenNotMember_ShouldReturnForbidden()
    {
        // Arrange
        var (masterClient, _, gameSystemId) = await CreateAuthenticatedClientWithGameSystemAsync(
            $"master-notmem-{Guid.NewGuid()}@example.com");
        var campaign = await CreateCampaignAsync(masterClient, gameSystemId);

        var authFactory = new AuthenticatedHttpClientFactory(_factory);
        var (otherClient, _, _) = await authFactory.CreateAuthenticatedClientAsync(
            $"other-notmem-{Guid.NewGuid()}@example.com");

        // Act
        var response = await otherClient.GetAsync($"/api/campaigns/{campaign.Id}/members");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    #endregion

    #region Update Member Role Tests

    [Fact]
    public async Task UpdateMemberRole_WhenOwner_ShouldReturnOk()
    {
        // Arrange - Master creates campaign
        var (masterClient, _, gameSystemId) = await CreateAuthenticatedClientWithGameSystemAsync(
            $"master-role-{Guid.NewGuid()}@example.com");
        var campaign = await CreateCampaignAsync(masterClient, gameSystemId);

        // Player joins
        var authFactory = new AuthenticatedHttpClientFactory(_factory);
        var (playerClient, _, _) = await authFactory.CreateAuthenticatedClientAsync(
            $"player-role-{Guid.NewGuid()}@example.com");
        await playerClient.PostAsJsonAsync("/api/campaigns/join", new { JoinCode = campaign.JoinCode });

        // Get members to find player's member ID
        var membersResponse = await masterClient.GetAsync($"/api/campaigns/{campaign.Id}/members");
        var members = await membersResponse.Content.ReadFromJsonAsync<List<CampaignMemberResponse>>();
        var playerMember = members!.First(m => m.Role == CampaignRole.Player);

        // Act - Promote player to Master
        var response = await masterClient.PatchAsJsonAsync(
            $"/api/campaigns/{campaign.Id}/members/{playerMember.Id}/role",
            new { Role = CampaignRole.Master });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var content = await response.Content.ReadFromJsonAsync<CampaignMemberResponse>();
        content!.Role.Should().Be(CampaignRole.Master);
    }

    [Fact]
    public async Task UpdateMemberRole_WhenNotOwner_ShouldReturnForbidden()
    {
        // Arrange - Master creates campaign
        var (masterClient, _, gameSystemId) = await CreateAuthenticatedClientWithGameSystemAsync(
            $"master-notown-{Guid.NewGuid()}@example.com");
        var campaign = await CreateCampaignAsync(masterClient, gameSystemId);

        // Player1 joins and gets promoted to Master
        var authFactory = new AuthenticatedHttpClientFactory(_factory);
        var (player1Client, _, _) = await authFactory.CreateAuthenticatedClientAsync(
            $"player1-notown-{Guid.NewGuid()}@example.com");
        await player1Client.PostAsJsonAsync("/api/campaigns/join", new { JoinCode = campaign.JoinCode });

        // Player2 joins
        var (player2Client, _, _) = await authFactory.CreateAuthenticatedClientAsync(
            $"player2-notown-{Guid.NewGuid()}@example.com");
        await player2Client.PostAsJsonAsync("/api/campaigns/join", new { JoinCode = campaign.JoinCode });

        // Get Player2's member ID
        var membersResponse = await masterClient.GetAsync($"/api/campaigns/{campaign.Id}/members");
        var members = await membersResponse.Content.ReadFromJsonAsync<List<CampaignMemberResponse>>();
        
        // Promote Player1 to Master first
        var player1Member = members!.First(m => m.DisplayName!.Contains("player1"));
        await masterClient.PatchAsJsonAsync(
            $"/api/campaigns/{campaign.Id}/members/{player1Member.Id}/role",
            new { Role = CampaignRole.Master });

        // Get updated members list
        membersResponse = await masterClient.GetAsync($"/api/campaigns/{campaign.Id}/members");
        members = await membersResponse.Content.ReadFromJsonAsync<List<CampaignMemberResponse>>();
        var player2Member = members!.First(m => m.DisplayName!.Contains("player2"));

        // Act - Player1 (not owner) tries to change Player2's role
        var response = await player1Client.PatchAsJsonAsync(
            $"/api/campaigns/{campaign.Id}/members/{player2Member.Id}/role",
            new { Role = CampaignRole.Master });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    #endregion

    #region Remove Member Tests

    [Fact]
    public async Task RemoveMember_WhenMaster_ShouldReturnNoContent()
    {
        // Arrange - Master creates campaign
        var (masterClient, _, gameSystemId) = await CreateAuthenticatedClientWithGameSystemAsync(
            $"master-remove-{Guid.NewGuid()}@example.com");
        var campaign = await CreateCampaignAsync(masterClient, gameSystemId);

        // Player joins
        var authFactory = new AuthenticatedHttpClientFactory(_factory);
        var (playerClient, _, _) = await authFactory.CreateAuthenticatedClientAsync(
            $"player-remove-{Guid.NewGuid()}@example.com");
        await playerClient.PostAsJsonAsync("/api/campaigns/join", new { JoinCode = campaign.JoinCode });

        // Get player's member ID
        var membersResponse = await masterClient.GetAsync($"/api/campaigns/{campaign.Id}/members");
        var members = await membersResponse.Content.ReadFromJsonAsync<List<CampaignMemberResponse>>();
        var playerMember = members!.First(m => m.Role == CampaignRole.Player);

        // Act - Master removes player
        var response = await masterClient.DeleteAsync(
            $"/api/campaigns/{campaign.Id}/members/{playerMember.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Verify player is removed
        membersResponse = await masterClient.GetAsync($"/api/campaigns/{campaign.Id}/members");
        members = await membersResponse.Content.ReadFromJsonAsync<List<CampaignMemberResponse>>();
        members.Should().HaveCount(1);
    }

    [Fact]
    public async Task RemoveMember_WhenPlayer_ShouldReturnForbidden()
    {
        // Arrange - Master creates campaign
        var (masterClient, _, gameSystemId) = await CreateAuthenticatedClientWithGameSystemAsync(
            $"master-rmnotauth-{Guid.NewGuid()}@example.com");
        var campaign = await CreateCampaignAsync(masterClient, gameSystemId);

        // Player1 joins
        var authFactory = new AuthenticatedHttpClientFactory(_factory);
        var (player1Client, _, _) = await authFactory.CreateAuthenticatedClientAsync(
            $"player1-rmnotauth-{Guid.NewGuid()}@example.com");
        await player1Client.PostAsJsonAsync("/api/campaigns/join", new { JoinCode = campaign.JoinCode });

        // Player2 joins
        var (player2Client, _, _) = await authFactory.CreateAuthenticatedClientAsync(
            $"player2-rmnotauth-{Guid.NewGuid()}@example.com");
        await player2Client.PostAsJsonAsync("/api/campaigns/join", new { JoinCode = campaign.JoinCode });

        // Get Player2's member ID
        var membersResponse = await masterClient.GetAsync($"/api/campaigns/{campaign.Id}/members");
        var members = await membersResponse.Content.ReadFromJsonAsync<List<CampaignMemberResponse>>();
        var player2Member = members!.First(m => m.DisplayName!.Contains("player2"));

        // Act - Player1 tries to remove Player2
        var response = await player1Client.DeleteAsync(
            $"/api/campaigns/{campaign.Id}/members/{player2Member.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    #endregion

    #region Regenerate Join Code Tests

    [Fact]
    public async Task RegenerateJoinCode_WhenMaster_ShouldReturnOk()
    {
        // Arrange
        var (client, _, gameSystemId) = await CreateAuthenticatedClientWithGameSystemAsync(
            $"regen-{Guid.NewGuid()}@example.com");
        var campaign = await CreateCampaignAsync(client, gameSystemId);
        var originalJoinCode = campaign.JoinCode;

        // Act
        var response = await client.PostAsync($"/api/campaigns/{campaign.Id}/regenerate-code", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var content = await response.Content.ReadFromJsonAsync<JoinCodeResponse>();
        content.Should().NotBeNull();
        content!.JoinCode.Should().NotBeNullOrEmpty();
        content.JoinCode.Should().HaveLength(8);
        content.JoinCode.Should().NotBe(originalJoinCode);
    }

    [Fact]
    public async Task RegenerateJoinCode_WhenPlayer_ShouldReturnForbidden()
    {
        // Arrange
        var (masterClient, _, gameSystemId) = await CreateAuthenticatedClientWithGameSystemAsync(
            $"master-regen-{Guid.NewGuid()}@example.com");
        var campaign = await CreateCampaignAsync(masterClient, gameSystemId);

        var authFactory = new AuthenticatedHttpClientFactory(_factory);
        var (playerClient, _, _) = await authFactory.CreateAuthenticatedClientAsync(
            $"player-regen-{Guid.NewGuid()}@example.com");
        await playerClient.PostAsJsonAsync("/api/campaigns/join", new { JoinCode = campaign.JoinCode });

        // Act
        var response = await playerClient.PostAsync($"/api/campaigns/{campaign.Id}/regenerate-code", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    #endregion

    #region Update Campaign Status Tests

    [Fact]
    public async Task UpdateCampaignStatus_WhenMaster_ShouldReturnOk()
    {
        // Arrange
        var (client, _, gameSystemId) = await CreateAuthenticatedClientWithGameSystemAsync(
            $"status-{Guid.NewGuid()}@example.com");
        var campaign = await CreateCampaignAsync(client, gameSystemId);

        // Act - Deactivate
        var deactivateResponse = await client.PatchAsJsonAsync(
            $"/api/campaigns/{campaign.Id}/status",
            new { IsActive = false });

        // Assert
        deactivateResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var content = await deactivateResponse.Content.ReadFromJsonAsync<CampaignResponse>();
        content!.IsActive.Should().BeFalse();

        // Act - Reactivate
        var activateResponse = await client.PatchAsJsonAsync(
            $"/api/campaigns/{campaign.Id}/status",
            new { IsActive = true });

        // Assert
        activateResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        
        content = await activateResponse.Content.ReadFromJsonAsync<CampaignResponse>();
        content!.IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task UpdateCampaignStatus_WhenPlayer_ShouldReturnForbidden()
    {
        // Arrange
        var (masterClient, _, gameSystemId) = await CreateAuthenticatedClientWithGameSystemAsync(
            $"master-status-{Guid.NewGuid()}@example.com");
        var campaign = await CreateCampaignAsync(masterClient, gameSystemId);

        var authFactory = new AuthenticatedHttpClientFactory(_factory);
        var (playerClient, _, _) = await authFactory.CreateAuthenticatedClientAsync(
            $"player-status-{Guid.NewGuid()}@example.com");
        await playerClient.PostAsJsonAsync("/api/campaigns/join", new { JoinCode = campaign.JoinCode });

        // Act
        var response = await playerClient.PatchAsJsonAsync(
            $"/api/campaigns/{campaign.Id}/status",
            new { IsActive = false });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    #endregion

    #region Access Control - Player Cannot See Join Code

    [Fact]
    public async Task GetCampaignById_WhenPlayer_ShouldNotSeeJoinCode()
    {
        // Arrange - Master creates campaign
        var (masterClient, _, gameSystemId) = await CreateAuthenticatedClientWithGameSystemAsync(
            $"master-nojoin-{Guid.NewGuid()}@example.com");
        var campaign = await CreateCampaignAsync(masterClient, gameSystemId);

        // Player joins
        var authFactory = new AuthenticatedHttpClientFactory(_factory);
        var (playerClient, _, _) = await authFactory.CreateAuthenticatedClientAsync(
            $"player-nojoin-{Guid.NewGuid()}@example.com");
        await playerClient.PostAsJsonAsync("/api/campaigns/join", new { JoinCode = campaign.JoinCode });

        // Act - Player gets campaign details
        var response = await playerClient.GetAsync($"/api/campaigns/{campaign.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var content = await response.Content.ReadFromJsonAsync<CampaignDetailResponse>();
        content!.JoinCode.Should().BeNull(); // Player should not see join code
        content.UserRole.Should().Be(CampaignRole.Player);
    }

    #endregion

    #region Response DTOs

    private record CampaignResponse(
        Guid Id,
        string Name,
        string? Description,
        Guid GameSystemId,
        bool IsActive,
        CampaignRole? UserRole,
        DateTime CreatedAt
    );

    private record CampaignDetailResponse(
        Guid Id,
        string Name,
        string? Description,
        Guid OwnerId,
        Guid GameSystemId,
        string? JoinCode,
        bool IsActive,
        Dictionary<string, object>? Settings,
        CampaignRole? UserRole,
        int MemberCount,
        DateTime CreatedAt,
        DateTime? UpdatedAt
    );

    private record CampaignMemberResponse(
        Guid Id,
        Guid UserId,
        string? DisplayName,
        CampaignRole Role,
        DateTime JoinedAt
    );

    private record JoinCodeResponse(string JoinCode);

    #endregion
}
