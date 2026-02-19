using System.Net;
using System.Net.Http.Json;
using Loremaster.Tests.Integration.Fixtures;

namespace Loremaster.Tests.Integration.Controllers;

public class AdminControllerTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;
    private string _adminToken = string.Empty;
    private string _userToken = string.Empty;

    public AdminControllerTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    private async Task SetupTokensAsync()
    {
        if (string.IsNullOrEmpty(_adminToken))
        {
            var adminRequest = new
            {
                Email = $"admin-{Guid.NewGuid()}@test.com",
                Password = "AdminPass123!",
                DisplayName = "Admin User",
                Role = "Admin"
            };
            
            var adminResponse = await _client.PostAsJsonAsync("/api/auth/register", adminRequest);
            var adminContent = await adminResponse.Content.ReadFromJsonAsync<RegisterResponse>();
            _adminToken = adminContent!.AccessToken;

            var userRequest = new
            {
                Email = $"user-{Guid.NewGuid()}@test.com",
                Password = "UserPass123!",
                DisplayName = "Regular User",
                Role = "Player"
            };
            
            var userResponse = await _client.PostAsJsonAsync("/api/auth/register", userRequest);
            var userContent = await userResponse.Content.ReadFromJsonAsync<RegisterResponse>();
            _userToken = userContent!.AccessToken;
        }
    }

    [Fact]
    public async Task GetAllUsers_WhenAdmin_ReturnsUsers()
    {
        await SetupTokensAsync();
        _client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _adminToken);

        var response = await _client.GetAsync("/api/admin/users");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadFromJsonAsync<List<UserResponse>>();
        content.Should().NotBeNull();
        content.Should().NotBeEmpty();
    }

    [Fact]
    public async Task GetAllUsers_WhenNotAdmin_ReturnsForbidden()
    {
        await SetupTokensAsync();
        _client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _userToken);

        var response = await _client.GetAsync("/api/admin/users");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task GetAllUsers_WhenNotAuthenticated_ReturnsUnauthorized()
    {
        var response = await _client.GetAsync("/api/admin/users");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetUserById_WhenAdmin_ReturnsUser()
    {
        await SetupTokensAsync();
        
        var adminRequest = new
        {
            Email = $"testuser-{Guid.NewGuid()}@test.com",
            Password = "UserPass123!",
            DisplayName = "Test User",
            Role = "Player"
        };
        var registerResponse = await _client.PostAsJsonAsync("/api/auth/register", adminRequest);
        var registerContent = await registerResponse.Content.ReadFromJsonAsync<RegisterResponse>();
        
        _client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _adminToken);

        var response = await _client.GetAsync($"/api/admin/users/{registerContent!.UserId}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadFromJsonAsync<UserDetailResponse>();
        content.Should().NotBeNull();
        content!.Email.Should().Be(adminRequest.Email);
    }

    [Fact]
    public async Task GetUserById_WhenNotFound_ReturnsNotFound()
    {
        await SetupTokensAsync();
        _client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _adminToken);

        var fakeId = Guid.NewGuid();
        var response = await _client.GetAsync($"/api/admin/users/{fakeId}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetAllCampaigns_WhenAdmin_ReturnsCampaigns()
    {
        await SetupTokensAsync();
        _client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _adminToken);

        var response = await _client.GetAsync("/api/admin/campaigns");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetAllCampaigns_WhenNotAdmin_ReturnsForbidden()
    {
        await SetupTokensAsync();
        _client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _userToken);

        var response = await _client.GetAsync("/api/admin/campaigns");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }
}

public class RegisterResponse
{
    public Guid UserId { get; set; }
    public string Email { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string AccessToken { get; set; } = string.Empty;
    public string RefreshToken { get; set; } = string.Empty;
}

public class UserResponse
{
    public Guid Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public bool IsActive { get; set; }
}

public class UserDetailResponse : UserResponse
{
    public DateTime? LastLoginAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public int OwnedCampaignsCount { get; set; }
    public int CampaignMembershipsCount { get; set; }
}
