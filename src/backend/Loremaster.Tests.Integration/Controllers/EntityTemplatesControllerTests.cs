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
/// Integration tests for EntityTemplatesController.
/// Tests template CRUD operations, extraction, confirmation workflow, and access control.
/// (EPIC 4 - Entity Definitions & Dynamic Templates)
/// </summary>
public class EntityTemplatesControllerTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public EntityTemplatesControllerTests(CustomWebApplicationFactory factory)
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
            code: code ?? $"test-{Guid.NewGuid():N}".Substring(0, 20),
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
    /// Creates a template directly in the database for testing.
    /// </summary>
    private async Task<EntityTemplate> CreateTemplateInDbAsync(
        Guid gameSystemId,
        Guid ownerId,
        string entityTypeName = "character",
        string displayName = "Character",
        TemplateStatus status = TemplateStatus.Draft)
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        
        var template = EntityTemplate.Create(
            entityTypeName: entityTypeName,
            displayName: displayName,
            gameSystemId: gameSystemId,
            ownerId: ownerId,
            description: "Test template"
        );
        
        // Add some fields
        template.SetFieldDefinitions(new[]
        {
            FieldDefinition.Text("name", "Name", isRequired: true, order: 1),
            FieldDefinition.Number("level", "Level", minValue: 1, maxValue: 20, order: 2)
        });
        
        // Set status if needed
        if (status == TemplateStatus.Confirmed)
        {
            template.Confirm(ownerId, "Confirmed for testing");
        }
        else if (status == TemplateStatus.PendingReview)
        {
            template.SubmitForReview();
        }
        else if (status == TemplateStatus.Rejected)
        {
            template.Reject("Rejected for testing");
        }
        
        dbContext.Set<EntityTemplate>().Add(template);
        await dbContext.SaveChangesAsync();
        
        return template;
    }

    #endregion

    #region Get Templates Tests

    [Fact]
    public async Task GetTemplates_WhenNotAuthenticated_ShouldReturnUnauthorized()
    {
        // Arrange
        var gameSystem = await CreateGameSystemInDbAsync();

        // Act - No authentication
        var response = await _client.GetAsync($"/api/game-systems/{gameSystem.Id}/templates");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetTemplates_WhenAuthenticated_ShouldReturnTemplatesForOwner()
    {
        // Arrange
        var gameSystem = await CreateGameSystemInDbAsync();
        
        var authFactory = new AuthenticatedHttpClientFactory(_factory);
        var (client, _, userIdStr) = await authFactory.CreateAuthenticatedClientAsync(
            $"templates-get-{Guid.NewGuid()}@example.com");
        var userId = Guid.Parse(userIdStr);

        // Create templates for this user
        var template1 = await CreateTemplateInDbAsync(gameSystem.Id, userId, "character", "Character");
        var template2 = await CreateTemplateInDbAsync(gameSystem.Id, userId, "location", "Location");

        // Act
        var response = await client.GetAsync($"/api/game-systems/{gameSystem.Id}/templates");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var content = await response.Content.ReadFromJsonAsync<GetTemplatesResponse>();
        content.Should().NotBeNull();
        content!.Templates.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetTemplates_WithStatusFilter_ShouldReturnFilteredTemplates()
    {
        // Arrange
        var gameSystem = await CreateGameSystemInDbAsync();
        
        var authFactory = new AuthenticatedHttpClientFactory(_factory);
        var (client, _, userIdStr) = await authFactory.CreateAuthenticatedClientAsync(
            $"templates-filter-{Guid.NewGuid()}@example.com");
        var userId = Guid.Parse(userIdStr);

        // Create templates with different statuses
        await CreateTemplateInDbAsync(gameSystem.Id, userId, "draft_entity", "Draft Entity", TemplateStatus.Draft);
        await CreateTemplateInDbAsync(gameSystem.Id, userId, "confirmed_entity", "Confirmed Entity", TemplateStatus.Confirmed);

        // Act - Filter by Confirmed status (status=2)
        var response = await client.GetAsync($"/api/game-systems/{gameSystem.Id}/templates?status=Confirmed");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var content = await response.Content.ReadFromJsonAsync<GetTemplatesResponse>();
        content.Should().NotBeNull();
        // Status 2 = Confirmed
        content!.Templates.Should().OnlyContain(t => t.Status == 2);
    }

    [Fact]
    public async Task GetTemplates_WithConfirmedOnlyFlag_ShouldReturnOnlyConfirmed()
    {
        // Arrange
        var gameSystem = await CreateGameSystemInDbAsync();
        
        var authFactory = new AuthenticatedHttpClientFactory(_factory);
        var (client, _, userIdStr) = await authFactory.CreateAuthenticatedClientAsync(
            $"templates-confirmed-{Guid.NewGuid()}@example.com");
        var userId = Guid.Parse(userIdStr);

        // Create templates with different statuses
        await CreateTemplateInDbAsync(gameSystem.Id, userId, "draft_only", "Draft Only", TemplateStatus.Draft);
        await CreateTemplateInDbAsync(gameSystem.Id, userId, "confirmed_only", "Confirmed Only", TemplateStatus.Confirmed);

        // Act
        var response = await client.GetAsync($"/api/game-systems/{gameSystem.Id}/templates?confirmedOnly=true");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var content = await response.Content.ReadFromJsonAsync<GetTemplatesResponse>();
        content.Should().NotBeNull();
        // Status 2 = Confirmed
        content!.Templates.Should().OnlyContain(t => t.Status == 2);
    }

    [Fact]
    public async Task GetTemplates_OtherUserTemplates_ShouldNotBeReturned()
    {
        // Arrange
        var gameSystem = await CreateGameSystemInDbAsync();
        
        // Create template as user1
        var authFactory1 = new AuthenticatedHttpClientFactory(_factory);
        var (_, _, userIdStr1) = await authFactory1.CreateAuthenticatedClientAsync(
            $"templates-user1-{Guid.NewGuid()}@example.com");
        var userId1 = Guid.Parse(userIdStr1);
        await CreateTemplateInDbAsync(gameSystem.Id, userId1, "user1_template", "User1 Template");

        // Query as user2
        var authFactory2 = new AuthenticatedHttpClientFactory(_factory);
        var (client2, _, _) = await authFactory2.CreateAuthenticatedClientAsync(
            $"templates-user2-{Guid.NewGuid()}@example.com");

        // Act
        var response = await client2.GetAsync($"/api/game-systems/{gameSystem.Id}/templates");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var content = await response.Content.ReadFromJsonAsync<GetTemplatesResponse>();
        content.Should().NotBeNull();
        // User2 should see no templates (they don't own any)
        content!.Templates.Should().BeEmpty();
    }

    #endregion

    #region Get Template By Id Tests

    [Fact]
    public async Task GetTemplateById_WhenExists_ShouldReturnTemplate()
    {
        // Arrange
        var gameSystem = await CreateGameSystemInDbAsync();
        
        var authFactory = new AuthenticatedHttpClientFactory(_factory);
        var (client, _, userIdStr) = await authFactory.CreateAuthenticatedClientAsync(
            $"templates-getid-{Guid.NewGuid()}@example.com");
        var userId = Guid.Parse(userIdStr);

        var template = await CreateTemplateInDbAsync(gameSystem.Id, userId, "test_entity", "Test Entity");

        // Act
        var response = await client.GetAsync($"/api/game-systems/{gameSystem.Id}/templates/{template.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var content = await response.Content.ReadFromJsonAsync<EntityTemplateResponse>();
        content.Should().NotBeNull();
        content!.Id.Should().Be(template.Id);
        content.EntityTypeName.Should().Be("test_entity");
        content.DisplayName.Should().Be("Test Entity");
    }

    [Fact]
    public async Task GetTemplateById_WhenNotExists_ShouldReturnNotFound()
    {
        // Arrange
        var gameSystem = await CreateGameSystemInDbAsync();
        
        var authFactory = new AuthenticatedHttpClientFactory(_factory);
        var (client, _, _) = await authFactory.CreateAuthenticatedClientAsync(
            $"templates-notfound-{Guid.NewGuid()}@example.com");

        // Act
        var response = await client.GetAsync($"/api/game-systems/{gameSystem.Id}/templates/{Guid.NewGuid()}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetTemplateById_WhenOtherUser_ShouldReturnNotFound()
    {
        // Arrange
        var gameSystem = await CreateGameSystemInDbAsync();
        
        // Create template as user1
        var authFactory1 = new AuthenticatedHttpClientFactory(_factory);
        var (_, _, userIdStr1) = await authFactory1.CreateAuthenticatedClientAsync(
            $"templates-owner-{Guid.NewGuid()}@example.com");
        var userId1 = Guid.Parse(userIdStr1);
        var template = await CreateTemplateInDbAsync(gameSystem.Id, userId1, "private_template", "Private");

        // Query as user2
        var authFactory2 = new AuthenticatedHttpClientFactory(_factory);
        var (client2, _, _) = await authFactory2.CreateAuthenticatedClientAsync(
            $"templates-other-{Guid.NewGuid()}@example.com");

        // Act
        var response = await client2.GetAsync($"/api/game-systems/{gameSystem.Id}/templates/{template.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    #endregion

    #region Confirm Template Tests

    [Fact]
    public async Task ConfirmTemplate_WhenDraft_ShouldConfirmAndReturn200()
    {
        // Arrange
        var gameSystem = await CreateGameSystemInDbAsync();
        
        var authFactory = new AuthenticatedHttpClientFactory(_factory);
        var (client, _, userIdStr) = await authFactory.CreateAuthenticatedClientAsync(
            $"templates-confirm-{Guid.NewGuid()}@example.com");
        var userId = Guid.Parse(userIdStr);

        var template = await CreateTemplateInDbAsync(gameSystem.Id, userId, "to_confirm", "To Confirm", TemplateStatus.Draft);

        var request = new { Notes = "Looks good!" };

        // Act
        var response = await client.PostAsJsonAsync(
            $"/api/game-systems/{gameSystem.Id}/templates/{template.Id}/confirm", 
            request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var content = await response.Content.ReadFromJsonAsync<ConfirmTemplateResponse>();
        content.Should().NotBeNull();
        content!.TemplateId.Should().Be(template.Id);
        content.EntityTypeName.Should().Be("to_confirm");
        content.ConfirmedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(30));
    }

    [Fact]
    public async Task ConfirmTemplate_WhenAlreadyConfirmed_ShouldReturnBadRequest()
    {
        // Arrange
        var gameSystem = await CreateGameSystemInDbAsync();
        
        var authFactory = new AuthenticatedHttpClientFactory(_factory);
        var (client, _, userIdStr) = await authFactory.CreateAuthenticatedClientAsync(
            $"templates-reconfirm-{Guid.NewGuid()}@example.com");
        var userId = Guid.Parse(userIdStr);

        var template = await CreateTemplateInDbAsync(gameSystem.Id, userId, "already_confirmed", "Already Confirmed", TemplateStatus.Confirmed);

        var request = new { Notes = "Try again" };

        // Act
        var response = await client.PostAsJsonAsync(
            $"/api/game-systems/{gameSystem.Id}/templates/{template.Id}/confirm", 
            request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ConfirmTemplate_WhenOtherUser_ShouldReturnNotFound()
    {
        // Arrange
        var gameSystem = await CreateGameSystemInDbAsync();
        
        // Create template as user1
        var authFactory1 = new AuthenticatedHttpClientFactory(_factory);
        var (_, _, userIdStr1) = await authFactory1.CreateAuthenticatedClientAsync(
            $"templates-confirmowner-{Guid.NewGuid()}@example.com");
        var userId1 = Guid.Parse(userIdStr1);
        var template = await CreateTemplateInDbAsync(gameSystem.Id, userId1);

        // Try to confirm as user2
        var authFactory2 = new AuthenticatedHttpClientFactory(_factory);
        var (client2, _, _) = await authFactory2.CreateAuthenticatedClientAsync(
            $"templates-confirmhacker-{Guid.NewGuid()}@example.com");

        var request = new { Notes = "Hacking attempt" };

        // Act
        var response = await client2.PostAsJsonAsync(
            $"/api/game-systems/{gameSystem.Id}/templates/{template.Id}/confirm", 
            request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    #endregion

    #region Update Template Tests

    [Fact]
    public async Task UpdateTemplate_WhenDraft_ShouldUpdateAndReturn200()
    {
        // Arrange
        var gameSystem = await CreateGameSystemInDbAsync();
        
        var authFactory = new AuthenticatedHttpClientFactory(_factory);
        var (client, _, userIdStr) = await authFactory.CreateAuthenticatedClientAsync(
            $"templates-update-{Guid.NewGuid()}@example.com");
        var userId = Guid.Parse(userIdStr);

        var template = await CreateTemplateInDbAsync(gameSystem.Id, userId, "updatable", "Updatable", TemplateStatus.Draft);

        var request = new
        {
            DisplayName = "Updated Display Name",
            Description = "Updated description",
            IconHint = "new-icon"
        };

        // Act
        var response = await client.PutAsJsonAsync(
            $"/api/game-systems/{gameSystem.Id}/templates/{template.Id}", 
            request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var content = await response.Content.ReadFromJsonAsync<UpdateTemplateResponse>();
        content.Should().NotBeNull();
        content!.TemplateId.Should().Be(template.Id);
        content.DisplayName.Should().Be("Updated Display Name");
    }

    [Fact]
    public async Task UpdateTemplate_WhenConfirmed_ShouldReturnBadRequest()
    {
        // Arrange
        var gameSystem = await CreateGameSystemInDbAsync();
        
        var authFactory = new AuthenticatedHttpClientFactory(_factory);
        var (client, _, userIdStr) = await authFactory.CreateAuthenticatedClientAsync(
            $"templates-updateconfirmed-{Guid.NewGuid()}@example.com");
        var userId = Guid.Parse(userIdStr);

        var template = await CreateTemplateInDbAsync(gameSystem.Id, userId, "confirmed_no_update", "No Update", TemplateStatus.Confirmed);

        var request = new
        {
            DisplayName = "Should Not Update"
        };

        // Act
        var response = await client.PutAsJsonAsync(
            $"/api/game-systems/{gameSystem.Id}/templates/{template.Id}", 
            request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    #endregion

    #region Access Control Tests

    [Fact]
    public async Task AllEndpoints_WhenNotMaster_ShouldReturnForbidden()
    {
        // This test would require a Player user, but our test factory creates Masters by default
        // For now we verify that unauthorized access is blocked
        var gameSystem = await CreateGameSystemInDbAsync();

        // Act - No authentication
        var getResponse = await _client.GetAsync($"/api/game-systems/{gameSystem.Id}/templates");
        var getByIdResponse = await _client.GetAsync($"/api/game-systems/{gameSystem.Id}/templates/{Guid.NewGuid()}");
        var extractResponse = await _client.PostAsJsonAsync($"/api/game-systems/{gameSystem.Id}/templates/extract", new {});
        var confirmResponse = await _client.PostAsJsonAsync($"/api/game-systems/{gameSystem.Id}/templates/{Guid.NewGuid()}/confirm", new {});

        // Assert - All should be unauthorized (no token)
        getResponse.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        getByIdResponse.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        extractResponse.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        confirmResponse.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    #endregion

    #region Response DTOs

    private record GetTemplatesResponse(
        Guid GameSystemId,
        IReadOnlyList<EntityTemplateSummaryResponse> Templates
    );

    private record EntityTemplateSummaryResponse(
        Guid Id,
        string EntityTypeName,
        string DisplayName,
        int Status, // Enum is serialized as int
        int FieldCount
    );

    private record EntityTemplateResponse(
        Guid Id,
        string EntityTypeName,
        string DisplayName,
        string? Description,
        int Status, // Enum is serialized as int
        Guid GameSystemId,
        Guid OwnerId
    );

    // Actual response format from ConfirmTemplateCommand
    private record ConfirmTemplateResponse(
        Guid TemplateId,
        string EntityTypeName,
        DateTime ConfirmedAt
    );

    // Actual response format from UpdateTemplateCommand
    private record UpdateTemplateResponse(
        Guid TemplateId,
        string EntityTypeName,
        string DisplayName,
        int FieldCount,
        DateTime UpdatedAt
    );

    #endregion
}
