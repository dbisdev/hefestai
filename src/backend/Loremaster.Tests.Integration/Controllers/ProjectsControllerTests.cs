using System.Net;
using System.Net.Http.Json;
using Loremaster.Tests.Integration.Fixtures;

namespace Loremaster.Tests.Integration.Controllers;

public class ProjectsControllerTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public ProjectsControllerTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    #region Create Project Tests

    [Fact]
    public async Task CreateProject_WhenAuthenticated_ShouldReturnCreated()
    {
        // Arrange
        var authFactory = new AuthenticatedHttpClientFactory(_factory);
        var (client, _, userId) = await authFactory.CreateAuthenticatedClientAsync(
            $"create-{Guid.NewGuid()}@example.com");

        var request = new
        {
            Name = "Test Project",
            Description = "A test project description"
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/projects", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        
        var content = await response.Content.ReadFromJsonAsync<ProjectResponse>();
        content.Should().NotBeNull();
        content!.Name.Should().Be(request.Name);
        content.Description.Should().Be(request.Description);
        content.OwnerId.ToString().Should().Be(userId);
    }

    [Fact]
    public async Task CreateProject_WhenNotAuthenticated_ShouldReturnUnauthorized()
    {
        // Arrange
        var request = new { Name = "Test Project", Description = "Description" };

        // Act
        var response = await _client.PostAsJsonAsync("/api/projects", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Theory]
    [InlineData("", "Description")]
    [InlineData(null, "Description")]
    public async Task CreateProject_WithInvalidName_ShouldReturnBadRequest(string? name, string description)
    {
        // Arrange
        var authFactory = new AuthenticatedHttpClientFactory(_factory);
        var (client, _, _) = await authFactory.CreateAuthenticatedClientAsync(
            $"invalid-{Guid.NewGuid()}@example.com");

        var request = new { Name = name, Description = description };

        // Act
        var response = await client.PostAsJsonAsync("/api/projects", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreateProject_WithDuplicateName_ShouldReturnBadRequest()
    {
        // Arrange
        var authFactory = new AuthenticatedHttpClientFactory(_factory);
        var (client, _, _) = await authFactory.CreateAuthenticatedClientAsync(
            $"duplicate-proj-{Guid.NewGuid()}@example.com");

        var request = new { Name = "Duplicate Project", Description = "Description" };

        // First project should succeed
        await client.PostAsJsonAsync("/api/projects", request);

        // Act - Second project with same name
        var response = await client.PostAsJsonAsync("/api/projects", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    #endregion

    #region Get Projects Tests

    [Fact]
    public async Task GetProjects_WhenAuthenticated_ShouldReturnOk()
    {
        // Arrange
        var authFactory = new AuthenticatedHttpClientFactory(_factory);
        var (client, _, _) = await authFactory.CreateAuthenticatedClientAsync(
            $"getprojects-{Guid.NewGuid()}@example.com");

        // Create some projects
        await client.PostAsJsonAsync("/api/projects", new { Name = "Project 1" });
        await client.PostAsJsonAsync("/api/projects", new { Name = "Project 2" });

        // Act
        var response = await client.GetAsync("/api/projects");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var content = await response.Content.ReadFromJsonAsync<List<ProjectListResponse>>();
        content.Should().NotBeNull();
        content.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetProjects_WhenNotAuthenticated_ShouldReturnUnauthorized()
    {
        // Act
        var response = await _client.GetAsync("/api/projects");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetProjects_ShouldOnlyReturnOwnProjects()
    {
        // Arrange
        var authFactory = new AuthenticatedHttpClientFactory(_factory);
        
        // User 1 creates a project
        var (client1, _, _) = await authFactory.CreateAuthenticatedClientAsync(
            $"user1-{Guid.NewGuid()}@example.com");
        await client1.PostAsJsonAsync("/api/projects", new { Name = "User1 Project" });

        // User 2 creates a project
        var (client2, _, _) = await authFactory.CreateAuthenticatedClientAsync(
            $"user2-{Guid.NewGuid()}@example.com");
        await client2.PostAsJsonAsync("/api/projects", new { Name = "User2 Project" });

        // Act - User 2 gets their projects
        var response = await client2.GetAsync("/api/projects");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var content = await response.Content.ReadFromJsonAsync<List<ProjectListResponse>>();
        content.Should().HaveCount(1);
        content![0].Name.Should().Be("User2 Project");
    }

    #endregion

    #region Get Project By Id Tests

    [Fact]
    public async Task GetProjectById_WithValidId_ShouldReturnOk()
    {
        // Arrange
        var authFactory = new AuthenticatedHttpClientFactory(_factory);
        var (client, _, _) = await authFactory.CreateAuthenticatedClientAsync(
            $"getbyid-{Guid.NewGuid()}@example.com");

        var createResponse = await client.PostAsJsonAsync("/api/projects", new { Name = "Get By Id Project" });
        var createdProject = await createResponse.Content.ReadFromJsonAsync<ProjectResponse>();

        // Act
        var response = await client.GetAsync($"/api/projects/{createdProject!.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var content = await response.Content.ReadFromJsonAsync<ProjectResponse>();
        content.Should().NotBeNull();
        content!.Id.Should().Be(createdProject.Id);
        content.Name.Should().Be("Get By Id Project");
    }

    [Fact]
    public async Task GetProjectById_WithNonExistentId_ShouldReturnNotFound()
    {
        // Arrange
        var authFactory = new AuthenticatedHttpClientFactory(_factory);
        var (client, _, _) = await authFactory.CreateAuthenticatedClientAsync(
            $"notfound-{Guid.NewGuid()}@example.com");

        // Act
        var response = await client.GetAsync($"/api/projects/{Guid.NewGuid()}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    #endregion

    #region Update Project Tests

    [Fact]
    public async Task UpdateProject_WithValidData_ShouldReturnOk()
    {
        // Arrange
        var authFactory = new AuthenticatedHttpClientFactory(_factory);
        var (client, _, _) = await authFactory.CreateAuthenticatedClientAsync(
            $"update-{Guid.NewGuid()}@example.com");

        var createResponse = await client.PostAsJsonAsync("/api/projects", new { Name = "Original Name" });
        var createdProject = await createResponse.Content.ReadFromJsonAsync<ProjectResponse>();

        var updateRequest = new { Name = "Updated Name", Description = "Updated Description" };

        // Act
        var response = await client.PutAsJsonAsync($"/api/projects/{createdProject!.Id}", updateRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var content = await response.Content.ReadFromJsonAsync<ProjectResponse>();
        content!.Name.Should().Be("Updated Name");
        content.Description.Should().Be("Updated Description");
    }

    #endregion

    #region Delete Project Tests

    [Fact]
    public async Task DeleteProject_WithValidId_ShouldReturnNoContent()
    {
        // Arrange
        var authFactory = new AuthenticatedHttpClientFactory(_factory);
        var (client, _, _) = await authFactory.CreateAuthenticatedClientAsync(
            $"delete-{Guid.NewGuid()}@example.com");

        var createResponse = await client.PostAsJsonAsync("/api/projects", new { Name = "To Delete" });
        var createdProject = await createResponse.Content.ReadFromJsonAsync<ProjectResponse>();

        // Act
        var response = await client.DeleteAsync($"/api/projects/{createdProject!.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Verify it's deleted
        var getResponse = await client.GetAsync($"/api/projects/{createdProject.Id}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    #endregion

    #region Archive/Restore Tests

    [Fact]
    public async Task ArchiveProject_ShouldReturnNoContent()
    {
        // Arrange
        var authFactory = new AuthenticatedHttpClientFactory(_factory);
        var (client, _, _) = await authFactory.CreateAuthenticatedClientAsync(
            $"archive-{Guid.NewGuid()}@example.com");

        var createResponse = await client.PostAsJsonAsync("/api/projects", new { Name = "To Archive" });
        var createdProject = await createResponse.Content.ReadFromJsonAsync<ProjectResponse>();

        // Act
        var response = await client.PostAsync($"/api/projects/{createdProject!.Id}/archive", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task RestoreProject_ShouldReturnNoContent()
    {
        // Arrange
        var authFactory = new AuthenticatedHttpClientFactory(_factory);
        var (client, _, _) = await authFactory.CreateAuthenticatedClientAsync(
            $"restore-{Guid.NewGuid()}@example.com");

        var createResponse = await client.PostAsJsonAsync("/api/projects", new { Name = "To Restore" });
        var createdProject = await createResponse.Content.ReadFromJsonAsync<ProjectResponse>();
        
        // Archive first
        await client.PostAsync($"/api/projects/{createdProject!.Id}/archive", null);

        // Act
        var response = await client.PostAsync($"/api/projects/{createdProject.Id}/restore", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    #endregion

    // Response DTOs
    private record ProjectResponse(Guid Id, string Name, string? Description, int Status, Guid OwnerId, DateTime CreatedAt);
    private record ProjectListResponse(Guid Id, string Name, string? Description, int Status, DateTime CreatedAt);
}
