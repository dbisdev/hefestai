using System.Net;
using System.Net.Http.Json;
using Loremaster.Domain.Entities;
using Loremaster.Domain.Enums;
using Loremaster.Domain.ValueObjects;
using Loremaster.Infrastructure.Persistence;
using Loremaster.Tests.Integration.Fixtures;
using Microsoft.Extensions.DependencyInjection;

namespace Loremaster.Tests.Integration.Controllers;

/// <summary>
/// Integration tests for EntitiesController.
/// Tests all lore entity CRUD operations and access control.
/// </summary>
public class EntitiesControllerTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public EntitiesControllerTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    #region Helper Methods

    /// <summary>
    /// Creates a game system in the test database and returns it.
    /// </summary>
    private async Task<GameSystem> CreateGameSystemAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        
        var gameSystem = GameSystem.Create(
            code: $"test-{Guid.NewGuid():N}".Substring(0, 20),
            name: "Test Game System",
            publisher: "Test Publisher",
            version: "1.0",
            description: "A test game system"
        );
        
        dbContext.GameSystems.Add(gameSystem);
        await dbContext.SaveChangesAsync();
        
        return gameSystem;
    }

    /// <summary>
    /// Creates a confirmed entity template in the database.
    /// Templates are owned by a Master and associated with a game system.
    /// </summary>
    /// <param name="gameSystemId">The game system ID for the template.</param>
    /// <param name="ownerId">The owner (Master) ID.</param>
    /// <param name="entityTypeName">The normalized entity type name (e.g., "character").</param>
    /// <param name="displayName">The display name (e.g., "Character").</param>
    /// <returns>The created and confirmed template.</returns>
    private async Task<EntityTemplate> CreateConfirmedTemplateAsync(
        Guid gameSystemId,
        Guid ownerId,
        string entityTypeName = "character",
        string displayName = "Character")
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        
        var template = EntityTemplate.Create(
            entityTypeName: entityTypeName,
            displayName: displayName,
            gameSystemId: gameSystemId,
            ownerId: ownerId,
            description: $"Template for {displayName} entities"
        );
        
        // Add basic field definitions
        template.SetFieldDefinitions(new[]
        {
            FieldDefinition.Text("name", "Name", isRequired: true, order: 1),
            FieldDefinition.TextArea("description", "Description", isRequired: false, order: 2)
        });
        
        // Confirm the template so it can be used for entity creation
        template.Confirm(ownerId, "Auto-confirmed for testing");
        
        dbContext.Set<EntityTemplate>().Add(template);
        await dbContext.SaveChangesAsync();
        
        return template;
    }

    /// <summary>
    /// Creates default confirmed templates (character, location) for entity creation tests.
    /// </summary>
    /// <param name="gameSystemId">The game system ID.</param>
    /// <param name="ownerId">The owner (Master) ID.</param>
    private async Task CreateDefaultTemplatesAsync(Guid gameSystemId, Guid ownerId)
    {
        await CreateConfirmedTemplateAsync(gameSystemId, ownerId, "character", "Character");
        await CreateConfirmedTemplateAsync(gameSystemId, ownerId, "location", "Location");
    }

    /// <summary>
    /// Creates an authenticated client with a campaign ready for entity operations.
    /// Also creates default confirmed templates (character, location) for entity creation.
    /// </summary>
    private async Task<(HttpClient Client, Guid CampaignId, string UserId, Guid GameSystemId)> CreateAuthenticatedClientWithCampaignAsync(
        string? email = null)
    {
        var authFactory = new AuthenticatedHttpClientFactory(_factory);
        var (client, _, userId) = await authFactory.CreateAuthenticatedClientAsync(
            email ?? $"entity-user-{Guid.NewGuid()}@example.com");
        
        var gameSystem = await CreateGameSystemAsync();
        
        // Create default confirmed templates for entity creation
        await CreateDefaultTemplatesAsync(gameSystem.Id, Guid.Parse(userId));
        
        // Create a campaign
        var campaignRequest = new
        {
            Name = "Test Campaign for Entities",
            GameSystemId = gameSystem.Id,
            Description = "A test campaign"
        };
        var campaignResponse = await client.PostAsJsonAsync("/api/campaigns", campaignRequest);
        campaignResponse.EnsureSuccessStatusCode();
        var campaign = await campaignResponse.Content.ReadFromJsonAsync<CampaignResponse>();
        
        return (client, campaign!.Id, userId, gameSystem.Id);
    }

    /// <summary>
    /// Helper to create an entity and return its response.
    /// </summary>
    private async Task<LoreEntityResponse> CreateEntityAsync(
        HttpClient client,
        Guid campaignId,
        string entityType = "character",
        string name = "Test Entity",
        string? description = null,
        VisibilityLevel? visibility = null)
    {
        var request = new
        {
            EntityType = entityType,
            Name = name,
            Description = description ?? "A test entity",
            Visibility = visibility
        };

        var response = await client.PostAsJsonAsync($"/api/campaigns/{campaignId}/entities", request);
        response.EnsureSuccessStatusCode();
        
        return (await response.Content.ReadFromJsonAsync<LoreEntityResponse>())!;
    }

    #endregion

    #region Create Entity Tests

    [Fact]
    public async Task CreateEntity_WhenMaster_ShouldReturnCreated()
    {
        // Arrange
        var (client, campaignId, userId, _) = await CreateAuthenticatedClientWithCampaignAsync(
            $"create-entity-{Guid.NewGuid()}@example.com");

        var request = new
        {
            EntityType = "character",
            Name = "Test Character",
            Description = "A brave adventurer"
        };

        // Act
        var response = await client.PostAsJsonAsync($"/api/campaigns/{campaignId}/entities", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        
        var content = await response.Content.ReadFromJsonAsync<LoreEntityResponse>();
        content.Should().NotBeNull();
        content!.Name.Should().Be(request.Name);
        content.Description.Should().Be(request.Description);
        content.EntityType.Should().Be("character");
        content.CampaignId.Should().Be(campaignId);
        content.OwnerId.ToString().Should().Be(userId);
        content.OwnershipType.Should().Be(OwnershipType.Master);
        content.Visibility.Should().Be(VisibilityLevel.Campaign);
    }

    [Fact]
    public async Task CreateEntity_WhenPlayer_ShouldCreateWithPlayerOwnership()
    {
        // Arrange - Master creates campaign
        var (masterClient, campaignId, _, _) = await CreateAuthenticatedClientWithCampaignAsync(
            $"master-create-{Guid.NewGuid()}@example.com");

        // Get join code
        var campaignResponse = await masterClient.GetAsync($"/api/campaigns/{campaignId}");
        var campaign = await campaignResponse.Content.ReadFromJsonAsync<CampaignDetailResponse>();

        // Player joins
        var authFactory = new AuthenticatedHttpClientFactory(_factory);
        var (playerClient, _, playerId) = await authFactory.CreateAuthenticatedClientAsync(
            $"player-create-{Guid.NewGuid()}@example.com");
        await playerClient.PostAsJsonAsync("/api/campaigns/join", new { JoinCode = campaign!.JoinCode });

        var request = new
        {
            EntityType = "character",
            Name = "Player's Character",
            Description = "My player character"
        };

        // Act
        var response = await playerClient.PostAsJsonAsync($"/api/campaigns/{campaignId}/entities", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        
        var content = await response.Content.ReadFromJsonAsync<LoreEntityResponse>();
        content!.OwnershipType.Should().Be(OwnershipType.Player);
        content.OwnerId.ToString().Should().Be(playerId);
    }

    [Fact]
    public async Task CreateEntity_WhenNotMember_ShouldReturnNotFound()
    {
        // Arrange - User 1 creates campaign
        var (_, campaignId, _, _) = await CreateAuthenticatedClientWithCampaignAsync(
            $"owner-{Guid.NewGuid()}@example.com");

        // User 2 (not a member) tries to create entity
        var authFactory = new AuthenticatedHttpClientFactory(_factory);
        var (otherClient, _, _) = await authFactory.CreateAuthenticatedClientAsync(
            $"other-{Guid.NewGuid()}@example.com");

        var request = new
        {
            EntityType = "character",
            Name = "Unauthorized Entity",
            Description = "Should fail"
        };

        // Act
        var response = await otherClient.PostAsJsonAsync($"/api/campaigns/{campaignId}/entities", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task CreateEntity_WhenNotAuthenticated_ShouldReturnUnauthorized()
    {
        // Act
        var response = await _client.PostAsJsonAsync(
            $"/api/campaigns/{Guid.NewGuid()}/entities",
            new { EntityType = "character", Name = "Test" });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Theory]
    [InlineData("", "Name is required")]
    [InlineData(null, "Name is required")]
    public async Task CreateEntity_WithInvalidName_ShouldReturnBadRequest(string? name, string _)
    {
        // Arrange
        var (client, campaignId, _, _) = await CreateAuthenticatedClientWithCampaignAsync(
            $"invalid-name-{Guid.NewGuid()}@example.com");

        var request = new
        {
            EntityType = "character",
            Name = name,
            Description = "Description"
        };

        // Act
        var response = await client.PostAsJsonAsync($"/api/campaigns/{campaignId}/entities", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    #endregion

    #region Get Entities Tests

    [Fact]
    public async Task GetCampaignEntities_WhenMember_ShouldReturnEntities()
    {
        // Arrange
        var (client, campaignId, _, _) = await CreateAuthenticatedClientWithCampaignAsync(
            $"get-entities-{Guid.NewGuid()}@example.com");

        // Create some entities
        await CreateEntityAsync(client, campaignId, name: "Entity 1");
        await CreateEntityAsync(client, campaignId, name: "Entity 2");

        // Act
        var response = await client.GetAsync($"/api/campaigns/{campaignId}/entities");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var content = await response.Content.ReadFromJsonAsync<GetEntitiesResponse>();
        content.Should().NotBeNull();
        content!.Items.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetCampaignEntities_WithTypeFilter_ShouldReturnFilteredEntities()
    {
        // Arrange
        var (client, campaignId, _, _) = await CreateAuthenticatedClientWithCampaignAsync(
            $"filter-type-{Guid.NewGuid()}@example.com");

        await CreateEntityAsync(client, campaignId, entityType: "character", name: "Character 1");
        await CreateEntityAsync(client, campaignId, entityType: "location", name: "Location 1");
        await CreateEntityAsync(client, campaignId, entityType: "character", name: "Character 2");

        // Act
        var response = await client.GetAsync($"/api/campaigns/{campaignId}/entities?entityType=character");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var content = await response.Content.ReadFromJsonAsync<GetEntitiesResponse>();
        content!.Items.Should().HaveCount(2);
        content.Items.All(e => e.EntityType == "character").Should().BeTrue();
    }

    [Fact]
    public async Task GetCampaignEntities_WhenNotMember_ShouldReturnNotFound()
    {
        // Arrange
        var (_, campaignId, _, _) = await CreateAuthenticatedClientWithCampaignAsync(
            $"owner-get-{Guid.NewGuid()}@example.com");

        var authFactory = new AuthenticatedHttpClientFactory(_factory);
        var (otherClient, _, _) = await authFactory.CreateAuthenticatedClientAsync(
            $"other-get-{Guid.NewGuid()}@example.com");

        // Act
        var response = await otherClient.GetAsync($"/api/campaigns/{campaignId}/entities");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    #endregion

    #region Get Entity By Id Tests

    [Fact]
    public async Task GetEntityById_WhenMember_ShouldReturnEntity()
    {
        // Arrange
        var (client, campaignId, _, _) = await CreateAuthenticatedClientWithCampaignAsync(
            $"get-by-id-{Guid.NewGuid()}@example.com");

        var entity = await CreateEntityAsync(client, campaignId, name: "Specific Entity");

        // Act
        var response = await client.GetAsync($"/api/campaigns/{campaignId}/entities/{entity.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var content = await response.Content.ReadFromJsonAsync<LoreEntityResponse>();
        content!.Id.Should().Be(entity.Id);
        content.Name.Should().Be("Specific Entity");
    }

    [Fact]
    public async Task GetEntityById_WhenNotMember_ShouldReturnNotFound()
    {
        // Arrange
        var (client, campaignId, _, _) = await CreateAuthenticatedClientWithCampaignAsync(
            $"owner-byid-{Guid.NewGuid()}@example.com");

        var entity = await CreateEntityAsync(client, campaignId);

        var authFactory = new AuthenticatedHttpClientFactory(_factory);
        var (otherClient, _, _) = await authFactory.CreateAuthenticatedClientAsync(
            $"other-byid-{Guid.NewGuid()}@example.com");

        // Act
        var response = await otherClient.GetAsync($"/api/campaigns/{campaignId}/entities/{entity.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetEntityById_WithNonExistentId_ShouldReturnNotFound()
    {
        // Arrange
        var (client, campaignId, _, _) = await CreateAuthenticatedClientWithCampaignAsync(
            $"notfound-{Guid.NewGuid()}@example.com");

        // Act
        var response = await client.GetAsync($"/api/campaigns/{campaignId}/entities/{Guid.NewGuid()}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    #endregion

    #region Update Entity Tests

    [Fact]
    public async Task UpdateEntity_WhenOwner_ShouldReturnOk()
    {
        // Arrange
        var (client, campaignId, _, _) = await CreateAuthenticatedClientWithCampaignAsync(
            $"update-{Guid.NewGuid()}@example.com");

        var entity = await CreateEntityAsync(client, campaignId, name: "Original Name");

        var updateRequest = new
        {
            Name = "Updated Name",
            Description = "Updated Description"
        };

        // Act
        var response = await client.PutAsJsonAsync(
            $"/api/campaigns/{campaignId}/entities/{entity.Id}", 
            updateRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var content = await response.Content.ReadFromJsonAsync<LoreEntityResponse>();
        content!.Name.Should().Be("Updated Name");
        content.Description.Should().Be("Updated Description");
    }

    [Fact]
    public async Task UpdateEntity_WhenMasterUpdatesOwnEntity_ShouldReturnOk()
    {
        // Arrange - Master creates campaign and another user becomes a second master
        var (masterClient, campaignId, _, _) = await CreateAuthenticatedClientWithCampaignAsync(
            $"master-update-{Guid.NewGuid()}@example.com");

        // Master creates entity (Master-owned)
        var entity = await CreateEntityAsync(masterClient, campaignId, 
            name: "Master Entity", 
            visibility: VisibilityLevel.Campaign);

        // Master updates their own entity
        var updateRequest = new
        {
            Name = "Master Updated",
            Description = "Updated by master"
        };

        // Act
        var response = await masterClient.PutAsJsonAsync(
            $"/api/campaigns/{campaignId}/entities/{entity.Id}", 
            updateRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var content = await response.Content.ReadFromJsonAsync<LoreEntityResponse>();
        content!.Name.Should().Be("Master Updated");
        content.Description.Should().Be("Updated by master");
    }

    [Fact]
    public async Task UpdateEntity_WhenMasterUpdatesPlayerEntity_ShouldReturnForbidden()
    {
        // Arrange - Master creates campaign and player joins
        var (masterClient, campaignId, _, _) = await CreateAuthenticatedClientWithCampaignAsync(
            $"master-forbid-player-{Guid.NewGuid()}@example.com");

        var campaignResponse = await masterClient.GetAsync($"/api/campaigns/{campaignId}");
        var campaign = await campaignResponse.Content.ReadFromJsonAsync<CampaignDetailResponse>();

        var authFactory = new AuthenticatedHttpClientFactory(_factory);
        var (playerClient, _, _) = await authFactory.CreateAuthenticatedClientAsync(
            $"player-forbid-{Guid.NewGuid()}@example.com");
        await playerClient.PostAsJsonAsync("/api/campaigns/join", new { JoinCode = campaign!.JoinCode });

        // Player creates entity (Player-owned, even with Campaign visibility)
        var entity = await CreateEntityAsync(playerClient, campaignId, 
            name: "Player Entity", 
            visibility: VisibilityLevel.Campaign);

        // Master tries to update player's entity - should be forbidden
        // because player-owned entities can only be edited by the player
        var updateRequest = new
        {
            Name = "Master Tried Update",
            Description = "Should not work"
        };

        // Act
        var response = await masterClient.PutAsJsonAsync(
            $"/api/campaigns/{campaignId}/entities/{entity.Id}", 
            updateRequest);

        // Assert - Player-owned entities cannot be edited by master (per domain rules)
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task UpdateEntity_WhenPlayerNotOwner_ShouldReturnForbidden()
    {
        // Arrange - Master creates campaign
        var (masterClient, campaignId, _, _) = await CreateAuthenticatedClientWithCampaignAsync(
            $"master-forbid-{Guid.NewGuid()}@example.com");

        // Master creates entity
        var entity = await CreateEntityAsync(masterClient, campaignId, name: "Master Entity");

        var campaignResponse = await masterClient.GetAsync($"/api/campaigns/{campaignId}");
        var campaign = await campaignResponse.Content.ReadFromJsonAsync<CampaignDetailResponse>();

        // Player joins and tries to update master's entity
        var authFactory = new AuthenticatedHttpClientFactory(_factory);
        var (playerClient, _, _) = await authFactory.CreateAuthenticatedClientAsync(
            $"player-forbid-{Guid.NewGuid()}@example.com");
        await playerClient.PostAsJsonAsync("/api/campaigns/join", new { JoinCode = campaign!.JoinCode });

        var updateRequest = new { Name = "Hacked", Description = "Unauthorized" };

        // Act
        var response = await playerClient.PutAsJsonAsync(
            $"/api/campaigns/{campaignId}/entities/{entity.Id}", 
            updateRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    #endregion

    #region Delete Entity Tests

    [Fact]
    public async Task DeleteEntity_WhenOwner_ShouldReturnNoContent()
    {
        // Arrange
        var (client, campaignId, _, _) = await CreateAuthenticatedClientWithCampaignAsync(
            $"delete-{Guid.NewGuid()}@example.com");

        var entity = await CreateEntityAsync(client, campaignId, name: "To Delete");

        // Act
        var response = await client.DeleteAsync($"/api/campaigns/{campaignId}/entities/{entity.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Verify it's deleted
        var getResponse = await client.GetAsync($"/api/campaigns/{campaignId}/entities/{entity.Id}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DeleteEntity_WhenPlayerNotOwner_ShouldReturnForbidden()
    {
        // Arrange - Master creates campaign and entity
        var (masterClient, campaignId, _, _) = await CreateAuthenticatedClientWithCampaignAsync(
            $"master-del-{Guid.NewGuid()}@example.com");

        var entity = await CreateEntityAsync(masterClient, campaignId);

        var campaignResponse = await masterClient.GetAsync($"/api/campaigns/{campaignId}");
        var campaign = await campaignResponse.Content.ReadFromJsonAsync<CampaignDetailResponse>();

        // Player joins and tries to delete
        var authFactory = new AuthenticatedHttpClientFactory(_factory);
        var (playerClient, _, _) = await authFactory.CreateAuthenticatedClientAsync(
            $"player-del-{Guid.NewGuid()}@example.com");
        await playerClient.PostAsJsonAsync("/api/campaigns/join", new { JoinCode = campaign!.JoinCode });

        // Act
        var response = await playerClient.DeleteAsync($"/api/campaigns/{campaignId}/entities/{entity.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    #endregion

    #region Change Visibility Tests

    [Fact]
    public async Task ChangeVisibility_WhenOwner_ShouldReturnOk()
    {
        // Arrange
        var (client, campaignId, _, _) = await CreateAuthenticatedClientWithCampaignAsync(
            $"visibility-{Guid.NewGuid()}@example.com");

        var entity = await CreateEntityAsync(client, campaignId, visibility: VisibilityLevel.Campaign);

        // Act
        var response = await client.PatchAsJsonAsync(
            $"/api/campaigns/{campaignId}/entities/{entity.Id}/visibility",
            new { Visibility = VisibilityLevel.Public });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var content = await response.Content.ReadFromJsonAsync<LoreEntityResponse>();
        content!.Visibility.Should().Be(VisibilityLevel.Public);
    }

    [Fact]
    public async Task ChangeVisibility_WhenPlayerNotOwner_ShouldReturnForbidden()
    {
        // Arrange - Master creates campaign and entity
        var (masterClient, campaignId, _, _) = await CreateAuthenticatedClientWithCampaignAsync(
            $"master-vis-{Guid.NewGuid()}@example.com");

        var entity = await CreateEntityAsync(masterClient, campaignId);

        var campaignResponse = await masterClient.GetAsync($"/api/campaigns/{campaignId}");
        var campaign = await campaignResponse.Content.ReadFromJsonAsync<CampaignDetailResponse>();

        // Player joins
        var authFactory = new AuthenticatedHttpClientFactory(_factory);
        var (playerClient, _, _) = await authFactory.CreateAuthenticatedClientAsync(
            $"player-vis-{Guid.NewGuid()}@example.com");
        await playerClient.PostAsJsonAsync("/api/campaigns/join", new { JoinCode = campaign!.JoinCode });

        // Act
        var response = await playerClient.PatchAsJsonAsync(
            $"/api/campaigns/{campaignId}/entities/{entity.Id}/visibility",
            new { Visibility = VisibilityLevel.Draft });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    #endregion

    #region Visibility Access Control Tests

    [Fact]
    public async Task GetEntity_WhenDraftVisibility_PlayerCannotSee()
    {
        // Arrange - Master creates campaign
        var (masterClient, campaignId, _, _) = await CreateAuthenticatedClientWithCampaignAsync(
            $"master-draft-{Guid.NewGuid()}@example.com");

        // Master creates draft entity
        var entity = await CreateEntityAsync(masterClient, campaignId, 
            name: "Draft Entity", 
            visibility: VisibilityLevel.Draft);

        var campaignResponse = await masterClient.GetAsync($"/api/campaigns/{campaignId}");
        var campaign = await campaignResponse.Content.ReadFromJsonAsync<CampaignDetailResponse>();

        // Player joins
        var authFactory = new AuthenticatedHttpClientFactory(_factory);
        var (playerClient, _, _) = await authFactory.CreateAuthenticatedClientAsync(
            $"player-draft-{Guid.NewGuid()}@example.com");
        await playerClient.PostAsJsonAsync("/api/campaigns/join", new { JoinCode = campaign!.JoinCode });

        // Act - Player tries to access draft entity
        var response = await playerClient.GetAsync($"/api/campaigns/{campaignId}/entities/{entity.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    #endregion

    #region Transfer Ownership Tests

    [Fact]
    public async Task TransferOwnership_WhenMasterTransfersToPlayer_ShouldReturnOk()
    {
        // Arrange - Master creates campaign
        var (masterClient, campaignId, _, _) = await CreateAuthenticatedClientWithCampaignAsync(
            $"master-transfer-{Guid.NewGuid()}@example.com");

        // Master creates entity
        var entity = await CreateEntityAsync(masterClient, campaignId, name: "Entity to Transfer");

        // Get join code
        var campaignResponse = await masterClient.GetAsync($"/api/campaigns/{campaignId}");
        var campaign = await campaignResponse.Content.ReadFromJsonAsync<CampaignDetailResponse>();

        // Player joins
        var authFactory = new AuthenticatedHttpClientFactory(_factory);
        var (_, _, playerId) = await authFactory.CreateAuthenticatedClientAsync(
            $"player-transfer-{Guid.NewGuid()}@example.com");
        
        // Need to get the player client to join
        var (playerClient, _, _) = await authFactory.CreateAuthenticatedClientAsync(
            $"player-transfer-{Guid.NewGuid()}@example.com");
        
        // Actually, let's get the player ID from joining
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        
        // Create player and add to campaign
        var playerUser = User.Create($"player-{Guid.NewGuid()}", $"player-transfer-target-{Guid.NewGuid()}@example.com", "Player");
        dbContext.Users.Add(playerUser);
        
        var membership = CampaignMember.Create(campaignId, playerUser.Id, CampaignRole.Player);
        dbContext.CampaignMembers.Add(membership);
        await dbContext.SaveChangesAsync();

        var transferRequest = new { NewOwnerId = playerUser.Id };

        // Act
        var response = await masterClient.PatchAsJsonAsync(
            $"/api/campaigns/{campaignId}/entities/{entity.Id}/owner",
            transferRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var content = await response.Content.ReadFromJsonAsync<LoreEntityResponse>();
        content!.OwnerId.Should().Be(playerUser.Id);
        content.OwnershipType.Should().Be(OwnershipType.Player);
    }

    [Fact]
    public async Task TransferOwnership_WhenMasterTransfersWithExplicitType_ShouldUseSpecifiedType()
    {
        // Arrange - Master creates campaign
        var (masterClient, campaignId, _, _) = await CreateAuthenticatedClientWithCampaignAsync(
            $"master-explicit-{Guid.NewGuid()}@example.com");

        var entity = await CreateEntityAsync(masterClient, campaignId, name: "Entity for Shared");

        // Create player in campaign
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        
        var playerUser = User.Create($"player-{Guid.NewGuid()}", $"player-shared-{Guid.NewGuid()}@example.com", "Player");
        dbContext.Users.Add(playerUser);
        
        var membership = CampaignMember.Create(campaignId, playerUser.Id, CampaignRole.Player);
        dbContext.CampaignMembers.Add(membership);
        await dbContext.SaveChangesAsync();

        var transferRequest = new 
        { 
            NewOwnerId = playerUser.Id,
            NewOwnershipType = OwnershipType.Shared 
        };

        // Act
        var response = await masterClient.PatchAsJsonAsync(
            $"/api/campaigns/{campaignId}/entities/{entity.Id}/owner",
            transferRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var content = await response.Content.ReadFromJsonAsync<LoreEntityResponse>();
        content!.OwnershipType.Should().Be(OwnershipType.Shared);
    }

    [Fact]
    public async Task TransferOwnership_WhenPlayerTries_ShouldReturnForbidden()
    {
        // Arrange - Master creates campaign
        var (masterClient, campaignId, _, _) = await CreateAuthenticatedClientWithCampaignAsync(
            $"master-forbid-transfer-{Guid.NewGuid()}@example.com");

        var entity = await CreateEntityAsync(masterClient, campaignId);

        var campaignResponse = await masterClient.GetAsync($"/api/campaigns/{campaignId}");
        var campaign = await campaignResponse.Content.ReadFromJsonAsync<CampaignDetailResponse>();

        // Player joins
        var authFactory = new AuthenticatedHttpClientFactory(_factory);
        var (playerClient, _, playerId) = await authFactory.CreateAuthenticatedClientAsync(
            $"player-forbid-transfer-{Guid.NewGuid()}@example.com");
        await playerClient.PostAsJsonAsync("/api/campaigns/join", new { JoinCode = campaign!.JoinCode });

        // Create another player to transfer to
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        
        var otherPlayer = User.Create($"other-{Guid.NewGuid()}", $"other-player-{Guid.NewGuid()}@example.com", "Other");
        dbContext.Users.Add(otherPlayer);
        
        var membership = CampaignMember.Create(campaignId, otherPlayer.Id, CampaignRole.Player);
        dbContext.CampaignMembers.Add(membership);
        await dbContext.SaveChangesAsync();

        var transferRequest = new { NewOwnerId = otherPlayer.Id };

        // Act - Player tries to transfer (should fail)
        var response = await playerClient.PatchAsJsonAsync(
            $"/api/campaigns/{campaignId}/entities/{entity.Id}/owner",
            transferRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task TransferOwnership_WhenNewOwnerNotCampaignMember_ShouldReturnNotFound()
    {
        // Arrange
        var (masterClient, campaignId, _, _) = await CreateAuthenticatedClientWithCampaignAsync(
            $"master-notmember-{Guid.NewGuid()}@example.com");

        var entity = await CreateEntityAsync(masterClient, campaignId);

        // Create a user who is NOT a campaign member
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        
        var nonMemberUser = User.Create($"nonmember-{Guid.NewGuid()}", $"nonmember-{Guid.NewGuid()}@example.com", "NonMember");
        dbContext.Users.Add(nonMemberUser);
        await dbContext.SaveChangesAsync();

        var transferRequest = new { NewOwnerId = nonMemberUser.Id };

        // Act
        var response = await masterClient.PatchAsJsonAsync(
            $"/api/campaigns/{campaignId}/entities/{entity.Id}/owner",
            transferRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task TransferOwnership_WhenEntityNotFound_ShouldReturnNotFound()
    {
        // Arrange
        var (masterClient, campaignId, _, _) = await CreateAuthenticatedClientWithCampaignAsync(
            $"master-entity-notfound-{Guid.NewGuid()}@example.com");

        // Create a player in campaign
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        
        var playerUser = User.Create($"player-{Guid.NewGuid()}", $"player-{Guid.NewGuid()}@example.com", "Player");
        dbContext.Users.Add(playerUser);
        
        var membership = CampaignMember.Create(campaignId, playerUser.Id, CampaignRole.Player);
        dbContext.CampaignMembers.Add(membership);
        await dbContext.SaveChangesAsync();

        var transferRequest = new { NewOwnerId = playerUser.Id };

        // Act - Try to transfer non-existent entity
        var response = await masterClient.PatchAsJsonAsync(
            $"/api/campaigns/{campaignId}/entities/{Guid.NewGuid()}/owner",
            transferRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task TransferOwnership_WhenNotAuthenticated_ShouldReturnUnauthorized()
    {
        // Act
        var response = await _client.PatchAsJsonAsync(
            $"/api/campaigns/{Guid.NewGuid()}/entities/{Guid.NewGuid()}/owner",
            new { NewOwnerId = Guid.NewGuid() });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task TransferOwnership_WhenMasterTransfersBetweenPlayers_ShouldSucceed()
    {
        // Arrange - Master creates campaign
        var (masterClient, campaignId, _, _) = await CreateAuthenticatedClientWithCampaignAsync(
            $"master-between-{Guid.NewGuid()}@example.com");

        // Get join code
        var campaignResponse = await masterClient.GetAsync($"/api/campaigns/{campaignId}");
        var campaign = await campaignResponse.Content.ReadFromJsonAsync<CampaignDetailResponse>();

        // First player joins and creates entity
        var authFactory = new AuthenticatedHttpClientFactory(_factory);
        var (player1Client, _, player1Id) = await authFactory.CreateAuthenticatedClientAsync(
            $"player1-between-{Guid.NewGuid()}@example.com");
        await player1Client.PostAsJsonAsync("/api/campaigns/join", new { JoinCode = campaign!.JoinCode });

        // Player 1 creates entity
        var entity = await CreateEntityAsync(player1Client, campaignId, name: "Player 1 Entity");

        // Second player joins
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        
        var player2User = User.Create($"player2-{Guid.NewGuid()}", $"player2-{Guid.NewGuid()}@example.com", "Player2");
        dbContext.Users.Add(player2User);
        
        var membership = CampaignMember.Create(campaignId, player2User.Id, CampaignRole.Player);
        dbContext.CampaignMembers.Add(membership);
        await dbContext.SaveChangesAsync();

        var transferRequest = new { NewOwnerId = player2User.Id };

        // Act - Master transfers from player 1 to player 2
        var response = await masterClient.PatchAsJsonAsync(
            $"/api/campaigns/{campaignId}/entities/{entity.Id}/owner",
            transferRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var content = await response.Content.ReadFromJsonAsync<LoreEntityResponse>();
        content!.OwnerId.Should().Be(player2User.Id);
        content.OwnershipType.Should().Be(OwnershipType.Player);
    }

    #endregion

    #region Template Validation Tests (EPIC 4 Regression)

    /// <summary>
    /// Verifies that creating an entity with a confirmed template succeeds.
    /// This is the core EPIC 4 requirement: entity types must be validated against templates.
    /// </summary>
    [Fact]
    public async Task CreateEntity_WithConfirmedTemplate_ShouldSucceed()
    {
        // Arrange - Campaign is created with default confirmed templates (character, location)
        var (client, campaignId, userId, _) = await CreateAuthenticatedClientWithCampaignAsync(
            $"template-success-{Guid.NewGuid()}@example.com");

        var request = new
        {
            EntityType = "character", // Template exists and is confirmed
            Name = "Template Validated Character",
            Description = "A character created with template validation"
        };

        // Act
        var response = await client.PostAsJsonAsync($"/api/campaigns/{campaignId}/entities", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        
        var content = await response.Content.ReadFromJsonAsync<LoreEntityResponse>();
        content.Should().NotBeNull();
        content!.Name.Should().Be(request.Name);
        content.EntityType.Should().Be("character");
        content.OwnerId.ToString().Should().Be(userId);
    }

    /// <summary>
    /// Verifies that creating an entity without a confirmed template fails with BadRequest.
    /// This ensures the template validation is enforced.
    /// </summary>
    [Fact]
    public async Task CreateEntity_WithoutConfirmedTemplate_ShouldReturnBadRequest()
    {
        // Arrange - Campaign has templates only for "character" and "location"
        var (client, campaignId, _, _) = await CreateAuthenticatedClientWithCampaignAsync(
            $"template-fail-{Guid.NewGuid()}@example.com");

        var request = new
        {
            EntityType = "monster", // No template exists for this type
            Name = "Invalid Monster",
            Description = "Should fail because no template exists"
        };

        // Act
        var response = await client.PostAsJsonAsync($"/api/campaigns/{campaignId}/entities", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("monster"); // Error message should mention the invalid entity type
    }

    /// <summary>
    /// Verifies that entity type names are normalized (e.g., "Player Character" becomes "player_character").
    /// </summary>
    [Fact]
    public async Task CreateEntity_WithNonNormalizedTypeName_ShouldNormalizeAndValidate()
    {
        // Arrange - Create a template with a normalized name
        var authFactory = new AuthenticatedHttpClientFactory(_factory);
        var (client, _, userIdStr) = await authFactory.CreateAuthenticatedClientAsync(
            $"template-normalize-{Guid.NewGuid()}@example.com");
        var userId = Guid.Parse(userIdStr);
        
        var gameSystem = await CreateGameSystemAsync();
        
        // Create template with normalized name "player_character"
        await CreateConfirmedTemplateAsync(gameSystem.Id, userId, "player_character", "Player Character");
        
        // Create campaign
        var campaignRequest = new
        {
            Name = "Normalize Test Campaign",
            GameSystemId = gameSystem.Id,
            Description = "Testing normalization"
        };
        var campaignResponse = await client.PostAsJsonAsync("/api/campaigns", campaignRequest);
        campaignResponse.EnsureSuccessStatusCode();
        var campaign = await campaignResponse.Content.ReadFromJsonAsync<CampaignResponse>();

        // Try to create entity with non-normalized name "Player Character"
        var request = new
        {
            EntityType = "Player Character", // Should be normalized to "player_character"
            Name = "My Player Character",
            Description = "Created with non-normalized type"
        };

        // Act
        var response = await client.PostAsJsonAsync($"/api/campaigns/{campaign!.Id}/entities", request);

        // Assert - Should succeed because the type name is normalized during validation
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        
        var content = await response.Content.ReadFromJsonAsync<LoreEntityResponse>();
        content!.EntityType.Should().Be("player_character"); // Should be normalized
    }

    /// <summary>
    /// Verifies that a draft template (not confirmed) cannot be used for entity creation.
    /// </summary>
    [Fact]
    public async Task CreateEntity_WithDraftTemplate_ShouldReturnBadRequest()
    {
        // Arrange - Create a campaign but add only a DRAFT template (not confirmed)
        var authFactory = new AuthenticatedHttpClientFactory(_factory);
        var (client, _, userIdStr) = await authFactory.CreateAuthenticatedClientAsync(
            $"template-draft-{Guid.NewGuid()}@example.com");
        var userId = Guid.Parse(userIdStr);
        
        var gameSystem = await CreateGameSystemAsync();
        
        // Create a DRAFT template (not confirmed)
        using (var scope = _factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            
            var draftTemplate = EntityTemplate.Create(
                entityTypeName: "draft_creature",
                displayName: "Draft Creature",
                gameSystemId: gameSystem.Id,
                ownerId: userId,
                description: "A draft template"
            );
            // Note: NOT calling Confirm() - template stays in Draft status
            
            dbContext.Set<EntityTemplate>().Add(draftTemplate);
            await dbContext.SaveChangesAsync();
        }
        
        // Create campaign
        var campaignRequest = new
        {
            Name = "Draft Template Test Campaign",
            GameSystemId = gameSystem.Id,
            Description = "Testing draft template rejection"
        };
        var campaignResponse = await client.PostAsJsonAsync("/api/campaigns", campaignRequest);
        campaignResponse.EnsureSuccessStatusCode();
        var campaign = await campaignResponse.Content.ReadFromJsonAsync<CampaignResponse>();

        // Try to create entity with the draft template type
        var request = new
        {
            EntityType = "draft_creature",
            Name = "Should Fail Creature",
            Description = "This should fail because template is not confirmed"
        };

        // Act
        var response = await client.PostAsJsonAsync($"/api/campaigns/{campaign!.Id}/entities", request);

        // Assert - Should fail because draft templates cannot be used
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    /// <summary>
    /// Verifies that templates from one user cannot be used by another user's campaign.
    /// Templates are per-owner, so each Master needs their own confirmed templates.
    /// </summary>
    [Fact]
    public async Task CreateEntity_WithOtherUserTemplate_ShouldReturnBadRequest()
    {
        // Arrange - User1 has confirmed template, User2 creates campaign without templates
        var authFactory1 = new AuthenticatedHttpClientFactory(_factory);
        var (_, _, userIdStr1) = await authFactory1.CreateAuthenticatedClientAsync(
            $"template-owner1-{Guid.NewGuid()}@example.com");
        var userId1 = Guid.Parse(userIdStr1);
        
        var gameSystem = await CreateGameSystemAsync();
        
        // User1 creates a confirmed template
        await CreateConfirmedTemplateAsync(gameSystem.Id, userId1, "owned_creature", "Owned Creature");
        
        // User2 creates campaign (does NOT have the template)
        var authFactory2 = new AuthenticatedHttpClientFactory(_factory);
        var (client2, _, _) = await authFactory2.CreateAuthenticatedClientAsync(
            $"template-owner2-{Guid.NewGuid()}@example.com");
        
        var campaignRequest = new
        {
            Name = "Other User Campaign",
            GameSystemId = gameSystem.Id,
            Description = "Testing template ownership"
        };
        var campaignResponse = await client2.PostAsJsonAsync("/api/campaigns", campaignRequest);
        campaignResponse.EnsureSuccessStatusCode();
        var campaign = await campaignResponse.Content.ReadFromJsonAsync<CampaignResponse>();

        // User2 tries to create entity using User1's template type
        var request = new
        {
            EntityType = "owned_creature",
            Name = "Should Fail",
            Description = "This should fail because template belongs to another user"
        };

        // Act
        var response = await client2.PostAsJsonAsync($"/api/campaigns/{campaign!.Id}/entities", request);

        // Assert - Should fail because User2 doesn't have a confirmed template for this type
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    /// <summary>
    /// Verifies that multiple entity types can be used when their templates are confirmed.
    /// </summary>
    [Fact]
    public async Task CreateEntities_WithMultipleConfirmedTemplates_ShouldSucceed()
    {
        // Arrange - Campaign has default templates for "character" and "location"
        var (client, campaignId, _, _) = await CreateAuthenticatedClientWithCampaignAsync(
            $"multi-template-{Guid.NewGuid()}@example.com");

        // Act - Create entities of different types
        var characterRequest = new { EntityType = "character", Name = "Test Character", Description = "A character" };
        var locationRequest = new { EntityType = "location", Name = "Test Location", Description = "A location" };

        var characterResponse = await client.PostAsJsonAsync($"/api/campaigns/{campaignId}/entities", characterRequest);
        var locationResponse = await client.PostAsJsonAsync($"/api/campaigns/{campaignId}/entities", locationRequest);

        // Assert - Both should succeed
        characterResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        locationResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        
        var character = await characterResponse.Content.ReadFromJsonAsync<LoreEntityResponse>();
        var location = await locationResponse.Content.ReadFromJsonAsync<LoreEntityResponse>();
        
        character!.EntityType.Should().Be("character");
        location!.EntityType.Should().Be("location");
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

    private record LoreEntityResponse(
        Guid Id,
        Guid CampaignId,
        Guid OwnerId,
        string EntityType,
        string Name,
        string? Description,
        OwnershipType OwnershipType,
        VisibilityLevel Visibility,
        bool IsTemplate,
        string? ImageUrl,
        Dictionary<string, object>? Attributes,
        Dictionary<string, object>? Metadata,
        DateTime CreatedAt,
        DateTime? UpdatedAt
    );

    /// <summary>
    /// Response wrapper for paginated entity lists matching GetCampaignEntitiesResult.
    /// </summary>
    private record GetEntitiesResponse(
        IReadOnlyCollection<LoreEntityResponse> Items,
        int TotalCount,
        int? PageNumber,
        int? TotalPages,
        bool HasNextPage,
        bool HasPreviousPage
    );

    #endregion
}
