using Loremaster.Application.Common.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Loremaster.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class AiController : ControllerBase
{
    private readonly IAiService _aiService;
    private readonly ILogger<AiController> _logger;

    public AiController(IAiService aiService, ILogger<AiController> logger)
    {
        _aiService = aiService;
        _logger = logger;
    }

    /// <summary>
    /// Generate a character with stats and description
    /// </summary>
    [HttpPost("generate/character")]
    [ProducesResponseType(typeof(CharacterGenerationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<CharacterGenerationResponse>> GenerateCharacter(
        [FromBody] CharacterGenerationRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Generating character: Species={Species}, Role={Role}", 
                request.Species, request.Role);

            var prompt = $@"Generate a sci-fi character based on:
Species: {request.Species}
Role: {request.Role}
Morphology: {request.Morphology}
Style: {request.Attire}

Respond with a JSON object containing:
- name: A unique sci-fi character name
- bio: A 2-3 sentence backstory
- stats: An object with STR (1-100), INT (1-100), DEX (1-100)

Example format:
{{""name"":""Zephyr-9"",""bio"":""A rogue android..."",""stats"":{{""STR"":75,""INT"":90,""DEX"":85}}}}";

            var textResult = await _aiService.GenerateJsonAsync(
                prompt,
                "You are a sci-fi character generator. Respond only with valid JSON.",
                0.8f,
                512,
                cancellationToken);

            // Generate image
            var imagePrompt = $"High-quality futuristic sci-fi portrait of a {request.Species} {request.Role}, {request.Morphology}, wearing {request.Attire}. Cinematic lighting, cyberpunk aesthetic, detailed face, 8k resolution, professional concept art, black background.";
            
            var imageResult = await _aiService.GenerateImageAsync(imagePrompt, cancellationToken);

            return Ok(new CharacterGenerationResponse
            {
                Success = true,
                CharacterJson = textResult.Json,
                ImageBase64 = imageResult.ImageBase64,
                ImageUrl = imageResult.ImageUrl
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate character");
            return StatusCode(500, new CharacterGenerationResponse
            {
                Success = false,
                Error = ex.Message
            });
        }
    }

    /// <summary>
    /// Generate a solar system with planets
    /// </summary>
    [HttpPost("generate/solar-system")]
    [ProducesResponseType(typeof(SolarSystemGenerationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<SolarSystemGenerationResponse>> GenerateSolarSystem(
        [FromBody] SolarSystemGenerationRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Generating solar system: SpectralClass={SpectralClass}, Planets={PlanetCount}", 
                request.SpectralClass, request.PlanetCount);

            var prompt = $@"Create a futuristic solar system with {request.PlanetCount} planets orbiting a {request.SpectralClass} class star.
Provide a unique sci-fi name, a brief overview of the system, and a name for each planet.

Respond with a JSON object containing:
- name: A unique star system name
- description: A 2-3 sentence description
- planets: An array of {request.PlanetCount} planet names

Example format:
{{""name"":""Nexus Prime"",""description"":""A binary system..."",""planets"":[""Arkon"",""Vela"",""Theron""]}}";

            var textResult = await _aiService.GenerateJsonAsync(
                prompt,
                "You are a sci-fi world builder. Respond only with valid JSON.",
                0.8f,
                512,
                cancellationToken);

            // Generate image
            var imagePrompt = $"Breathtaking wide-angle cinematic view of a {request.SpectralClass}-type star solar system. Visible planets orbiting, vibrant cosmic nebulas in background, high detail, photorealistic space photography, sci-fi concept art, deep blacks, vivid colors.";
            
            var imageResult = await _aiService.GenerateImageAsync(imagePrompt, cancellationToken);

            return Ok(new SolarSystemGenerationResponse
            {
                Success = true,
                SystemJson = textResult.Json,
                ImageBase64 = imageResult.ImageBase64,
                ImageUrl = imageResult.ImageUrl
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate solar system");
            return StatusCode(500, new SolarSystemGenerationResponse
            {
                Success = false,
                Error = ex.Message
            });
        }
    }

    /// <summary>
    /// Generate a vehicle with stats
    /// </summary>
    [HttpPost("generate/vehicle")]
    [ProducesResponseType(typeof(VehicleGenerationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<VehicleGenerationResponse>> GenerateVehicle(
        [FromBody] VehicleGenerationRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Generating vehicle: Type={Type}, Class={Class}", 
                request.Type, request.Class);

            var prompt = $@"Create a futuristic vehicle:
Type: {request.Type}
Class: {request.Class}
Engine: {request.Engine}

Respond with a JSON object containing:
- name: A unique vehicle designation/name
- specs: A 2-3 sentence description of capabilities
- stats: An object with SPEED (1-100), ARMOR (1-100), CARGO (1-100)

Example format:
{{""name"":""Phantom-X7"",""specs"":""A stealth interceptor..."",""stats"":{{""SPEED"":95,""ARMOR"":40,""CARGO"":20}}}}";

            var textResult = await _aiService.GenerateJsonAsync(
                prompt,
                "You are a sci-fi vehicle designer. Respond only with valid JSON.",
                0.8f,
                512,
                cancellationToken);

            return Ok(new VehicleGenerationResponse
            {
                Success = true,
                VehicleJson = textResult.Json
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate vehicle");
            return StatusCode(500, new VehicleGenerationResponse
            {
                Success = false,
                Error = ex.Message
            });
        }
    }

    /// <summary>
    /// Test the Genkit AI service connection
    /// </summary>
    [HttpPost("test")]
    [ProducesResponseType(typeof(TestAiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<TestAiResponse>> TestGenkit(
        [FromBody] TestAiRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Testing Genkit AI service with prompt: {Prompt}", request.Prompt);

            var result = await _aiService.GenerateTextAsync(
                request.Prompt,
                request.SystemPrompt,
                request.Temperature ?? 0.7f,
                request.MaxTokens ?? 256,
                cancellationToken);

            return Ok(new TestAiResponse
            {
                Success = true,
                Text = result.Text,
                Usage = result.Usage != null ? new TokenUsageResponse
                {
                    PromptTokens = result.Usage.PromptTokens,
                    CompletionTokens = result.Usage.CompletionTokens,
                    TotalTokens = result.Usage.TotalTokens
                } : null
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to test Genkit AI service");
            return StatusCode(500, new TestAiResponse
            {
                Success = false,
                Error = ex.Message
            });
        }
    }

    /// <summary>
    /// Check if the Genkit AI service is healthy
    /// </summary>
    [HttpGet("health")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(HealthResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<HealthResponse>> CheckHealth(CancellationToken cancellationToken)
    {
        var isHealthy = await _aiService.IsHealthyAsync(cancellationToken);
        
        return Ok(new HealthResponse
        {
            IsHealthy = isHealthy,
            Service = "Genkit AI",
            Timestamp = DateTime.UtcNow
        });
    }
}

// Request/Response DTOs
public record CharacterGenerationRequest
{
    public string Species { get; init; } = "human";
    public string Role { get; init; } = "operative";
    public string Morphology { get; init; } = "NEUTRAL";
    public string Attire { get; init; } = "Techwear";
}

public record CharacterGenerationResponse
{
    public bool Success { get; init; }
    public string? CharacterJson { get; init; }
    public string? ImageBase64 { get; init; }
    public string? ImageUrl { get; init; }
    public string? Error { get; init; }
}

public record SolarSystemGenerationRequest
{
    public string SpectralClass { get; init; } = "G";
    public int PlanetCount { get; init; } = 8;
}

public record SolarSystemGenerationResponse
{
    public bool Success { get; init; }
    public string? SystemJson { get; init; }
    public string? ImageBase64 { get; init; }
    public string? ImageUrl { get; init; }
    public string? Error { get; init; }
}

public record VehicleGenerationRequest
{
    public string Type { get; init; } = "starship";
    public string Class { get; init; } = "interceptor";
    public string Engine { get; init; } = "fusion";
}

public record VehicleGenerationResponse
{
    public bool Success { get; init; }
    public string? VehicleJson { get; init; }
    public string? Error { get; init; }
}

public record TestAiRequest
{
    public string Prompt { get; init; } = "Hello, can you confirm you are working?";
    public string? SystemPrompt { get; init; }
    public float? Temperature { get; init; }
    public int? MaxTokens { get; init; }
}

public record TestAiResponse
{
    public bool Success { get; init; }
    public string? Text { get; init; }
    public string? Error { get; init; }
    public TokenUsageResponse? Usage { get; init; }
}

public record TokenUsageResponse
{
    public int PromptTokens { get; init; }
    public int CompletionTokens { get; init; }
    public int TotalTokens { get; init; }
}

public record HealthResponse
{
    public bool IsHealthy { get; init; }
    public string Service { get; init; } = null!;
    public DateTime Timestamp { get; init; }
}
