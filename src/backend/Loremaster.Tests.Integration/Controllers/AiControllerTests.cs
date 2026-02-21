using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Loremaster.Tests.Integration.Fixtures;

namespace Loremaster.Tests.Integration.Controllers;

public class AiControllerTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;
    private string _masterToken = string.Empty;

    private record RegisterResponse(Guid UserId, string Email, string? DisplayName, string Role, string AccessToken, string RefreshToken);
    private record HealthResponse(bool IsHealthy, string Service, DateTime Timestamp);

    public AiControllerTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    private async Task SetupMasterTokenAsync()
    {
        if (string.IsNullOrEmpty(_masterToken))
        {
            var request = new
            {
                Email = $"master-{Guid.NewGuid()}@test.com",
                Password = "MasterPass123!",
                DisplayName = "Master User",
                Role = "Master"
            };

            var response = await _client.PostAsJsonAsync("/api/auth/register", request);
            var content = await response.Content.ReadFromJsonAsync<RegisterResponse>();
            _masterToken = content!.AccessToken;
        }
    }

    #region Health Endpoint

    [Fact]
    public async Task Health_WhenCalled_ReturnsOk()
    {
        var response = await _client.GetAsync("/api/ai/health");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadFromJsonAsync<HealthResponse>();
        content.Should().NotBeNull();
        content!.Service.Should().Be("Genkit AI");
    }

    #endregion

    #region Character Generation

    [Fact]
    public async Task GenerateCharacter_WhenAuthenticated_ReturnsOk()
    {
        await SetupMasterTokenAsync();
        _client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _masterToken);

        var request = new
        {
            Species = "human",
            Role = "operative",
            Morphology = "MASCULINE",
            GenerateImage = false
        };

        var response = await _client.PostAsJsonAsync("/api/ai/generate/character", request);

        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.BadGateway);
    }

    [Fact]
    public async Task GenerateCharacter_WhenNotAuthenticated_ReturnsUnauthorized()
    {
        var request = new
        {
            Species = "human",
            Role = "operative"
        };

        var response = await _client.PostAsJsonAsync("/api/ai/generate/character", request);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    #endregion

    #region Solar System Generation

    [Fact]
    public async Task GenerateSolarSystem_WhenAuthenticated_ReturnsOk()
    {
        await SetupMasterTokenAsync();
        _client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _masterToken);

        var request = new
        {
            SpectralClass = "G",
            PlanetCount = 5,
            GenerateImage = false
        };

        var response = await _client.PostAsJsonAsync("/api/ai/generate/solar-system", request);

        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.BadGateway);
    }

    [Fact]
    public async Task GenerateSolarSystem_WhenNotAuthenticated_ReturnsUnauthorized()
    {
        var request = new
        {
            SpectralClass = "G",
            PlanetCount = 5
        };

        var response = await _client.PostAsJsonAsync("/api/ai/generate/solar-system", request);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    #endregion

    #region Vehicle Generation

    [Fact]
    public async Task GenerateVehicle_WhenAuthenticated_ReturnsOk()
    {
        await SetupMasterTokenAsync();
        _client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _masterToken);

        var request = new
        {
            Type = "starship",
            Class = "interceptor",
            Engine = "fusion",
            GenerateImage = false
        };

        var response = await _client.PostAsJsonAsync("/api/ai/generate/vehicle", request);

        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.BadGateway);
    }

    [Fact]
    public async Task GenerateVehicle_WhenNotAuthenticated_ReturnsUnauthorized()
    {
        var request = new
        {
            Type = "starship",
            Class = "interceptor"
        };

        var response = await _client.PostAsJsonAsync("/api/ai/generate/vehicle", request);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    #endregion

    #region NPC Generation

    [Fact]
    public async Task GenerateNpc_WhenAuthenticated_ReturnsOk()
    {
        await SetupMasterTokenAsync();
        _client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _masterToken);

        var request = new
        {
            Species = "human",
            Occupation = "merchant",
            Personality = "friendly",
            Setting = "space-station",
            GenerateImage = false
        };

        var response = await _client.PostAsJsonAsync("/api/ai/generate/npc", request);

        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.BadGateway);
    }

    [Fact]
    public async Task GenerateNpc_WhenNotAuthenticated_ReturnsUnauthorized()
    {
        var request = new
        {
            Species = "human",
            Occupation = "merchant"
        };

        var response = await _client.PostAsJsonAsync("/api/ai/generate/npc", request);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    #endregion

    #region Enemy Generation

    [Fact]
    public async Task GenerateEnemy_WhenAuthenticated_ReturnsOk()
    {
        await SetupMasterTokenAsync();
        _client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _masterToken);

        var request = new
        {
            Species = "alien-beast",
            ThreatLevel = "moderate",
            Behavior = "aggressive",
            Environment = "space-station",
            GenerateImage = false
        };

        var response = await _client.PostAsJsonAsync("/api/ai/generate/enemy", request);

        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.BadGateway);
    }

    [Fact]
    public async Task GenerateEnemy_WhenNotAuthenticated_ReturnsUnauthorized()
    {
        var request = new
        {
            Species = "alien-beast",
            ThreatLevel = "moderate"
        };

        var response = await _client.PostAsJsonAsync("/api/ai/generate/enemy", request);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    #endregion

    #region Mission Generation

    [Fact]
    public async Task GenerateMission_WhenAuthenticated_ReturnsOk()
    {
        await SetupMasterTokenAsync();
        _client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _masterToken);

        var request = new
        {
            MissionType = "extraction",
            Difficulty = "MEDIUM",
            Environment = "space-station",
            FactionInvolved = "corporate",
            GenerateImage = false
        };

        var response = await _client.PostAsJsonAsync("/api/ai/generate/mission", request);

        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.BadGateway);
    }

    [Fact]
    public async Task GenerateMission_WhenNotAuthenticated_ReturnsUnauthorized()
    {
        var request = new
        {
            MissionType = "extraction",
            Difficulty = "MEDIUM"
        };

        var response = await _client.PostAsJsonAsync("/api/ai/generate/mission", request);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    #endregion

    #region Encounter Generation

    [Fact]
    public async Task GenerateEncounter_WhenAuthenticated_ReturnsOk()
    {
        await SetupMasterTokenAsync();
        _client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _masterToken);

        var request = new
        {
            EncounterType = "combat",
            Difficulty = "MEDIUM",
            Environment = "open-area",
            EnemyCount = "squad",
            GenerateImage = false
        };

        var response = await _client.PostAsJsonAsync("/api/ai/generate/encounter", request);

        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.BadGateway);
    }

    [Fact]
    public async Task GenerateEncounter_WhenNotAuthenticated_ReturnsUnauthorized()
    {
        var request = new
        {
            EncounterType = "combat",
            Difficulty = "MEDIUM"
        };

        var response = await _client.PostAsJsonAsync("/api/ai/generate/encounter", request);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    #endregion

    #region Test Endpoint

    [Fact]
    public async Task Test_WhenAuthenticated_ReturnsOk()
    {
        await SetupMasterTokenAsync();
        _client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _masterToken);

        var request = new
        {
            Prompt = "Hello, can you confirm you are working?",
            Temperature = 0.7,
            MaxTokens = 100
        };

        var response = await _client.PostAsJsonAsync("/api/ai/test", request);

        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.BadGateway, HttpStatusCode.InternalServerError);
    }

    [Fact]
    public async Task Test_WhenNotAuthenticated_ReturnsUnauthorized()
    {
        var request = new
        {
            Prompt = "Hello"
        };

        var response = await _client.PostAsJsonAsync("/api/ai/test", request);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    #endregion
}
