using System.Net;
using System.Net.Http.Json;
using Loremaster.Tests.Integration.Fixtures;

namespace Loremaster.Tests.Integration.Controllers;

public class AuthControllerTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public AuthControllerTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    #region Register Tests

    [Fact]
    public async Task Register_WithValidData_ShouldReturnOkWithTokens()
    {
        // Arrange
        var request = new
        {
            Email = $"newuser-{Guid.NewGuid()}@example.com",
            Password = "ValidPassword123!",
            DisplayName = "New User"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/register", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var content = await response.Content.ReadFromJsonAsync<RegisterResponse>();
        content.Should().NotBeNull();
        content!.Email.Should().Be(request.Email);
        content.DisplayName.Should().Be(request.DisplayName);
        content.AccessToken.Should().NotBeNullOrEmpty();
        content.RefreshToken.Should().NotBeNullOrEmpty();
        content.Role.Should().Be("User");
    }

    [Theory]
    [InlineData("", "Password123!", "Display Name")]
    [InlineData("invalid-email", "Password123!", "Display Name")]
    [InlineData("valid@email.com", "", "Display Name")]
    [InlineData("valid@email.com", "short", "Display Name")]
    public async Task Register_WithInvalidData_ShouldReturnBadRequest(string email, string password, string displayName)
    {
        // Arrange
        var request = new { Email = email, Password = password, DisplayName = displayName };

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/register", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Register_WithDuplicateEmail_ShouldReturnBadRequest()
    {
        // Arrange
        var email = $"duplicate-{Guid.NewGuid()}@example.com";
        var request = new
        {
            Email = email,
            Password = "ValidPassword123!",
            DisplayName = "User"
        };

        // First registration should succeed
        await _client.PostAsJsonAsync("/api/auth/register", request);

        // Act - Second registration with same email
        var response = await _client.PostAsJsonAsync("/api/auth/register", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    #endregion

    #region Login Tests

    [Fact]
    public async Task Login_WithValidCredentials_ShouldReturnOkWithTokens()
    {
        // Arrange
        var email = $"logintest-{Guid.NewGuid()}@example.com";
        var password = "ValidPassword123!";
        
        // Register first
        await _client.PostAsJsonAsync("/api/auth/register", new
        {
            Email = email,
            Password = password,
            DisplayName = "Login Test User"
        });

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/login", new
        {
            Email = email,
            Password = password
        });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var content = await response.Content.ReadFromJsonAsync<LoginResponse>();
        content.Should().NotBeNull();
        content!.Email.Should().Be(email);
        content.AccessToken.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task Login_WithInvalidPassword_ShouldReturnBadRequest()
    {
        // Arrange
        var email = $"wrongpass-{Guid.NewGuid()}@example.com";
        
        // Register first
        await _client.PostAsJsonAsync("/api/auth/register", new
        {
            Email = email,
            Password = "CorrectPassword123!",
            DisplayName = "User"
        });

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/login", new
        {
            Email = email,
            Password = "WrongPassword123!"
        });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Login_WithNonExistentUser_ShouldReturnBadRequest()
    {
        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/login", new
        {
            Email = "nonexistent@example.com",
            Password = "SomePassword123!"
        });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    #endregion

    #region GetCurrentUser Tests

    [Fact]
    public async Task GetCurrentUser_WhenAuthenticated_ShouldReturnUserInfo()
    {
        // Arrange
        var authFactory = new AuthenticatedHttpClientFactory(_factory);
        var (client, _, _) = await authFactory.CreateAuthenticatedClientAsync(
            $"currentuser-{Guid.NewGuid()}@example.com");

        // Act
        var response = await client.GetAsync("/api/auth/me");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var content = await response.Content.ReadFromJsonAsync<CurrentUserResponse>();
        content.Should().NotBeNull();
        content!.Email.Should().Contain("currentuser");
    }

    [Fact]
    public async Task GetCurrentUser_WhenNotAuthenticated_ShouldReturnUnauthorized()
    {
        // Act
        var response = await _client.GetAsync("/api/auth/me");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    #endregion

    #region Logout Tests

    [Fact]
    public async Task Logout_WhenAuthenticated_ShouldReturnNoContent()
    {
        // Arrange
        var authFactory = new AuthenticatedHttpClientFactory(_factory);
        var (client, _, _) = await authFactory.CreateAuthenticatedClientAsync(
            $"logout-{Guid.NewGuid()}@example.com");

        // Act
        var response = await client.PostAsync("/api/auth/logout", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task Logout_WhenNotAuthenticated_ShouldReturnUnauthorized()
    {
        // Act
        var response = await _client.PostAsync("/api/auth/logout", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    #endregion

    // Response DTOs
    private record RegisterResponse(Guid UserId, string Email, string? DisplayName, string Role, string AccessToken, string RefreshToken);
    private record LoginResponse(Guid UserId, string Email, string? DisplayName, string Role, string AccessToken, string RefreshToken);
    private record CurrentUserResponse(Guid UserId, string Email, string? DisplayName, string Role);
}
