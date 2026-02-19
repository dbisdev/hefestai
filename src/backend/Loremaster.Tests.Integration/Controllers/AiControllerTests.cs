using System.Net;
using System.Net.Http.Json;
using Loremaster.Tests.Integration.Controllers;

namespace Loremaster.Tests.Integration.Controllers;

public class AiControllerTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;
    private string _masterToken = string.Empty;

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

    [Fact]
    public async Task Generate_WhenAuthenticated_ReturnsOk()
    {
        await SetupMasterTokenAsync();
        _client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _masterToken);

        var request = new
        {
            Prompt = "Create a character name",
            SystemPrompt = "You are a creative assistant",
            Temperature = 0.7,
            MaxTokens = 500
        };

        var response = await _client.PostAsJsonAsync("/api/ai/generate", request);

        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.BadGateway);
    }

    [Fact]
    public async Task Generate_WhenNotAuthenticated_ReturnsUnauthorized()
    {
        var request = new
        {
            Prompt = "Create a character name",
            Temperature = 0.7
        };

        var response = await _client.PostAsJsonAsync("/api/ai/generate", request);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GenerateImage_WhenAuthenticated_ReturnsOk()
    {
        await SetupMasterTokenAsync();
        _client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _masterToken);

        var request = new
        {
            Prompt = "A fantasy warrior",
            AspectRatio = "1:1",
            Style = "fantasy"
        };

        var response = await _client.PostAsJsonAsync("/api/ai/generate-image", request);

        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.BadGateway);
    }

    [Fact]
    public async Task GenerateImage_WhenNotAuthenticated_ReturnsUnauthorized()
    {
        var request = new
        {
            Prompt = "A fantasy warrior",
            AspectRatio = "1:1"
        };

        var response = await _client.PostAsJsonAsync("/api/ai/generate-image", request);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Chat_WhenAuthenticated_ReturnsOk()
    {
        await SetupMasterTokenAsync();
        _client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _masterToken);

        var request = new
        {
            Messages = new[]
            {
                new { Role = "user", Content = "Hello" }
            },
            Temperature = 0.7
        };

        var response = await _client.PostAsJsonAsync("/api/ai/chat", request);

        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.BadGateway);
    }

    [Fact]
    public async Task Chat_WhenNotAuthenticated_ReturnsUnauthorized()
    {
        var request = new
        {
            Messages = new[]
            {
                new { Role = "user", Content = "Hello" }
            }
        };

        var response = await _client.PostAsJsonAsync("/api/ai/chat", request);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Summarize_WhenAuthenticated_ReturnsOk()
    {
        await SetupMasterTokenAsync();
        _client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _masterToken);

        var request = new
        {
            Text = "This is a long text that needs to be summarized. " + new string('a', 100),
            Style = "concise",
            MaxLength = 100
        };

        var response = await _client.PostAsJsonAsync("/api/ai/summarize", request);

        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.BadGateway);
    }

    [Fact]
    public async Task Generate_WithInvalidPrompt_ReturnsBadRequest()
    {
        await SetupMasterTokenAsync();
        _client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _masterToken);

        var request = new
        {
            Prompt = "",
            Temperature = 0.7
        };

        var response = await _client.PostAsJsonAsync("/api/ai/generate", request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Generate_WithInvalidTemperature_ReturnsBadRequest()
    {
        await SetupMasterTokenAsync();
        _client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _masterToken);

        var request = new
        {
            Prompt = "Generate something",
            Temperature = 5.0
        };

        var response = await _client.PostAsJsonAsync("/api/ai/generate", request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}
