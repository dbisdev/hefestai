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
/// Integration tests for EntityGenerationController.
/// Tests AI-assisted entity generation with RAG.
/// (EPIC 4.5 - Entity Assisted Generation)
/// </summary>
public class EntityGenerationControllerTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public EntityGenerationControllerTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    #region Helper Methods

    /// <summary>
    /// Creates a game system directly in the database for testing.
    /// </summary>
    private async Task<GameSystem> CreateGameSystemInDbAsync(string? code = null)
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        
        var gameSystem = GameSystem.Create(
            code: code ?? $"gen-{Guid.NewGuid():N}"[..20],
            name: "Test Game System",
            ownerId: Guid.NewGuid(),
            publisher: "Test Publisher",
            version: "1.0",
            description: "A test game system for generation"
        );
        
        dbContext.GameSystems.Add(gameSystem);
        await dbContext.SaveChangesAsync();
        
        return gameSystem;
    }

    /// <summary>
    /// Creates a campaign directly in the database for testing.
    /// </summary>
    private async Task<Campaign> CreateCampaignInDbAsync(Guid ownerId, Guid gameSystemId, string name = "Test Campaign")
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        
        var campaign = Campaign.Create(
            ownerId: ownerId,
            gameSystemId: gameSystemId,
            name: name,
            description: "A test campaign for generation tests"
        );
        
        dbContext.Set<Campaign>().Add(campaign);
        await dbContext.SaveChangesAsync();
        
        return campaign;
    }

    /// <summary>
    /// Creates a campaign member directly in the database for testing.
    /// </summary>
    private async Task<CampaignMember> CreateCampaignMemberInDbAsync(
        Guid campaignId, 
        Guid userId, 
        CampaignRole role = CampaignRole.Master)
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        
        var member = CampaignMember.Create(campaignId, userId, role);
        
        dbContext.Set<CampaignMember>().Add(member);
        await dbContext.SaveChangesAsync();
        
        return member;
    }

    /// <summary>
    /// Creates a template directly in the database for testing.
    /// </summary>
    private async Task<EntityTemplate> CreateConfirmedTemplateInDbAsync(
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
            description: "Test template for generation"
        );
        
        // Add fields
        template.SetFieldDefinitions(new[]
        {
            FieldDefinition.Text("name", "Name", isRequired: true, order: 1),
            FieldDefinition.TextArea("background", "Background", order: 2),
            FieldDefinition.Number("level", "Level", minValue: 1, maxValue: 20, order: 3),
            FieldDefinition.Select("race", "Race", new[] { "Human", "Elf", "Dwarf" }, order: 4)
        });
        
        // Confirm the template
        template.Confirm(ownerId, "Confirmed for testing");
        
        dbContext.Set<EntityTemplate>().Add(template);
        await dbContext.SaveChangesAsync();
        
        return template;
    }

    /// <summary>
    /// Creates an entity directly in the database for testing.
    /// </summary>
    private async Task<LoreEntity> CreateEntityInDbAsync(
        Guid campaignId,
        Guid ownerId,
        string entityType = "character",
        string name = "Test Entity")
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        
        var entity = LoreEntity.Create(
            campaignId: campaignId,
            ownerId: ownerId,
            entityType: entityType,
            name: name,
            description: "A test entity for generation tests"
        );
        
        dbContext.Set<LoreEntity>().Add(entity);
        await dbContext.SaveChangesAsync();
        
        return entity;
    }

    #endregion

    #region GenerateEntityFields Tests

    [Fact]
    public async Task GenerateEntityFields_WhenNotAuthenticated_ShouldReturnUnauthorized()
    {
        // Arrange
        var campaignId = Guid.NewGuid();
        var request = new GenerateEntityFieldsRequest
        {
            EntityTypeName = "character",
            UserPrompt = "A brave warrior"
        };

        // Act - No authentication
        var response = await _client.PostAsJsonAsync(
            $"/api/campaigns/{campaignId}/generation/fields", 
            request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GenerateEntityFields_WhenNotCampaignMember_ShouldReturnNotFound()
    {
        // Arrange
        var gameSystem = await CreateGameSystemInDbAsync();
        
        // Create campaign as user1
        var authFactory1 = new AuthenticatedHttpClientFactory(_factory);
        var (_, _, userIdStr1) = await authFactory1.CreateAuthenticatedClientAsync(
            $"gen-owner-{Guid.NewGuid()}@example.com");
        var userId1 = Guid.Parse(userIdStr1);
        
        var campaign = await CreateCampaignInDbAsync(userId1, gameSystem.Id);
        await CreateCampaignMemberInDbAsync(campaign.Id, userId1, CampaignRole.Master);

        // Request as user2 (not a member)
        var authFactory2 = new AuthenticatedHttpClientFactory(_factory);
        var (client2, _, _) = await authFactory2.CreateAuthenticatedClientAsync(
            $"gen-other-{Guid.NewGuid()}@example.com");

        var request = new GenerateEntityFieldsRequest
        {
            EntityTypeName = "character",
            UserPrompt = "A brave warrior"
        };

        // Act
        var response = await client2.PostAsJsonAsync(
            $"/api/campaigns/{campaign.Id}/generation/fields", 
            request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GenerateEntityFields_WhenNoConfirmedTemplate_ShouldReturnSuccessWithError()
    {
        // Arrange
        var gameSystem = await CreateGameSystemInDbAsync();
        
        var authFactory = new AuthenticatedHttpClientFactory(_factory);
        var (client, _, userIdStr) = await authFactory.CreateAuthenticatedClientAsync(
            $"gen-notemplate-{Guid.NewGuid()}@example.com");
        var userId = Guid.Parse(userIdStr);
        
        var campaign = await CreateCampaignInDbAsync(userId, gameSystem.Id);
        await CreateCampaignMemberInDbAsync(campaign.Id, userId, CampaignRole.Master);

        // Note: No template created - this should fail gracefully
        var request = new GenerateEntityFieldsRequest
        {
            EntityTypeName = "character",
            UserPrompt = "A brave warrior"
        };

        // Act
        var response = await client.PostAsJsonAsync(
            $"/api/campaigns/{campaign.Id}/generation/fields", 
            request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var content = await response.Content.ReadFromJsonAsync<GenerateEntityFieldsResponse>();
        content.Should().NotBeNull();
        content!.Success.Should().BeFalse();
        content.ErrorMessage.Should().Contain("template");
    }

    [Fact]
    public async Task GenerateEntityFields_WithInvalidTemperature_ShouldReturnBadRequest()
    {
        // Arrange
        var gameSystem = await CreateGameSystemInDbAsync();
        
        var authFactory = new AuthenticatedHttpClientFactory(_factory);
        var (client, _, userIdStr) = await authFactory.CreateAuthenticatedClientAsync(
            $"gen-badtemp-{Guid.NewGuid()}@example.com");
        var userId = Guid.Parse(userIdStr);
        
        var campaign = await CreateCampaignInDbAsync(userId, gameSystem.Id);
        await CreateCampaignMemberInDbAsync(campaign.Id, userId, CampaignRole.Master);

        var request = new GenerateEntityFieldsRequest
        {
            EntityTypeName = "character",
            Temperature = 2.0f // Invalid - must be 0.0-1.0
        };

        // Act
        var response = await client.PostAsJsonAsync(
            $"/api/campaigns/{campaign.Id}/generation/fields", 
            request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GenerateEntityFields_WithEmptyEntityTypeName_ShouldReturnBadRequest()
    {
        // Arrange
        var gameSystem = await CreateGameSystemInDbAsync();
        
        var authFactory = new AuthenticatedHttpClientFactory(_factory);
        var (client, _, userIdStr) = await authFactory.CreateAuthenticatedClientAsync(
            $"gen-notype-{Guid.NewGuid()}@example.com");
        var userId = Guid.Parse(userIdStr);
        
        var campaign = await CreateCampaignInDbAsync(userId, gameSystem.Id);
        await CreateCampaignMemberInDbAsync(campaign.Id, userId, CampaignRole.Master);

        var request = new GenerateEntityFieldsRequest
        {
            EntityTypeName = "" // Invalid - required
        };

        // Act
        var response = await client.PostAsJsonAsync(
            $"/api/campaigns/{campaign.Id}/generation/fields", 
            request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    #endregion

    #region GenerateEntityImage Tests

    [Fact]
    public async Task GenerateEntityImage_WhenNotAuthenticated_ShouldReturnUnauthorized()
    {
        // Arrange
        var campaignId = Guid.NewGuid();
        var entityId = Guid.NewGuid();
        var request = new GenerateEntityImageRequest
        {
            Style = "fantasy"
        };

        // Act - No authentication
        var response = await _client.PostAsJsonAsync(
            $"/api/campaigns/{campaignId}/generation/entities/{entityId}/image", 
            request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GenerateEntityImage_WhenEntityNotFound_ShouldReturnNotFound()
    {
        // Arrange
        var gameSystem = await CreateGameSystemInDbAsync();
        
        var authFactory = new AuthenticatedHttpClientFactory(_factory);
        var (client, _, userIdStr) = await authFactory.CreateAuthenticatedClientAsync(
            $"gen-img-notfound-{Guid.NewGuid()}@example.com");
        var userId = Guid.Parse(userIdStr);
        
        var campaign = await CreateCampaignInDbAsync(userId, gameSystem.Id);
        await CreateCampaignMemberInDbAsync(campaign.Id, userId, CampaignRole.Master);

        var request = new GenerateEntityImageRequest
        {
            Style = "fantasy"
        };

        // Act - Entity doesn't exist
        var response = await client.PostAsJsonAsync(
            $"/api/campaigns/{campaign.Id}/generation/entities/{Guid.NewGuid()}/image", 
            request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GenerateEntityImage_WhenNotOwnerOrMaster_ShouldReturnForbidden()
    {
        // Arrange
        var gameSystem = await CreateGameSystemInDbAsync();
        
        // Create campaign and entity as user1 (Master)
        var authFactory1 = new AuthenticatedHttpClientFactory(_factory);
        var (_, _, userIdStr1) = await authFactory1.CreateAuthenticatedClientAsync(
            $"gen-img-owner-{Guid.NewGuid()}@example.com");
        var userId1 = Guid.Parse(userIdStr1);
        
        var campaign = await CreateCampaignInDbAsync(userId1, gameSystem.Id);
        await CreateCampaignMemberInDbAsync(campaign.Id, userId1, CampaignRole.Master);
        
        // Create entity as player-owned
        var authFactory2 = new AuthenticatedHttpClientFactory(_factory);
        var (client2, _, userIdStr2) = await authFactory2.CreateAuthenticatedClientAsync(
            $"gen-img-player-{Guid.NewGuid()}@example.com");
        var userId2 = Guid.Parse(userIdStr2);
        await CreateCampaignMemberInDbAsync(campaign.Id, userId2, CampaignRole.Player);
        
        // Entity owned by user1 (Master)
        var entity = await CreateEntityInDbAsync(campaign.Id, userId1);

        var request = new GenerateEntityImageRequest
        {
            Style = "fantasy"
        };

        // Act - Player trying to generate image for Master's entity
        var response = await client2.PostAsJsonAsync(
            $"/api/campaigns/{campaign.Id}/generation/entities/{entity.Id}/image", 
            request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task GenerateEntityImage_WithInvalidStyle_ShouldReturnBadRequest()
    {
        // Arrange
        var gameSystem = await CreateGameSystemInDbAsync();
        
        var authFactory = new AuthenticatedHttpClientFactory(_factory);
        var (client, _, userIdStr) = await authFactory.CreateAuthenticatedClientAsync(
            $"gen-img-badstyle-{Guid.NewGuid()}@example.com");
        var userId = Guid.Parse(userIdStr);
        
        var campaign = await CreateCampaignInDbAsync(userId, gameSystem.Id);
        await CreateCampaignMemberInDbAsync(campaign.Id, userId, CampaignRole.Master);
        var entity = await CreateEntityInDbAsync(campaign.Id, userId);

        var request = new GenerateEntityImageRequest
        {
            Style = "invalid_style" // Must be fantasy, realistic, anime, or sketch
        };

        // Act
        var response = await client.PostAsJsonAsync(
            $"/api/campaigns/{campaign.Id}/generation/entities/{entity.Id}/image", 
            request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    #endregion

    #region RegenerateEntityImage Tests

    [Fact]
    public async Task RegenerateEntityImage_WhenNotAuthenticated_ShouldReturnUnauthorized()
    {
        // Arrange
        var campaignId = Guid.NewGuid();
        var entityId = Guid.NewGuid();
        var request = new RegenerateEntityImageRequest
        {
            Style = "anime"
        };

        // Act - No authentication
        var response = await _client.PostAsJsonAsync(
            $"/api/campaigns/{campaignId}/generation/entities/{entityId}/image/regenerate", 
            request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task RegenerateEntityImage_WithNullBody_ShouldAcceptAndUseDefaults()
    {
        // Arrange
        var gameSystem = await CreateGameSystemInDbAsync();
        
        var authFactory = new AuthenticatedHttpClientFactory(_factory);
        var (client, _, userIdStr) = await authFactory.CreateAuthenticatedClientAsync(
            $"gen-regen-null-{Guid.NewGuid()}@example.com");
        var userId = Guid.Parse(userIdStr);
        
        var campaign = await CreateCampaignInDbAsync(userId, gameSystem.Id);
        await CreateCampaignMemberInDbAsync(campaign.Id, userId, CampaignRole.Master);
        await CreateConfirmedTemplateInDbAsync(gameSystem.Id, userId);
        var entity = await CreateEntityInDbAsync(campaign.Id, userId);

        // Act - Null body should be acceptable
        var response = await client.PostAsJsonAsync<RegenerateEntityImageRequest?>(
            $"/api/campaigns/{campaign.Id}/generation/entities/{entity.Id}/image/regenerate", 
            null);

        // Assert - Should not return BadRequest (might fail at AI service level, but that's OK)
        response.StatusCode.Should().NotBe(HttpStatusCode.BadRequest);
    }

    #endregion

    #region Request DTOs

    private record GenerateEntityFieldsRequest
    {
        public string EntityTypeName { get; init; } = null!;
        public string? UserPrompt { get; init; }
        public List<string>? FieldsToGenerate { get; init; }
        public Dictionary<string, object?>? ExistingValues { get; init; }
        public float? Temperature { get; init; }
        public bool? IncludeImageGeneration { get; init; }
        public string? ImageStyle { get; init; }
    }

    private record GenerateEntityImageRequest
    {
        public string? Style { get; init; }
        public bool? IsRegeneration { get; init; }
    }

    private record RegenerateEntityImageRequest
    {
        public string? Style { get; init; }
    }

    #endregion

    #region Response DTOs

    private record GenerateEntityFieldsResponse(
        bool Success,
        Dictionary<string, object?> GeneratedFields,
        string? SuggestedName,
        string? SuggestedDescription,
        string? ImageDataUrl,
        string? ImageUrl,
        string? ErrorMessage,
        Guid TemplateId,
        string EntityTypeName
    );

    private record GenerateEntityImageResponse(
        bool Success,
        string? ImageBase64,
        string? ImageDataUrl,
        string? StoredImageUrl,
        string? GeneratedPrompt,
        string? ErrorMessage,
        Guid EntityId
    );

    #endregion
}
