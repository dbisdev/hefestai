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
            
            var imageResult = await _aiService.GenerateImageAsync(imagePrompt, cancellationToken: cancellationToken);

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
            
            var imageResult = await _aiService.GenerateImageAsync(imagePrompt, cancellationToken: cancellationToken);

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
    /// Generate an NPC (Non-Player Character) with personality and background
    /// </summary>
    [HttpPost("generate/npc")]
    [ProducesResponseType(typeof(NpcGenerationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<NpcGenerationResponse>> GenerateNpc(
        [FromBody] NpcGenerationRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Generating NPC: Species={Species}, Occupation={Occupation}, Personality={Personality}", 
                request.Species, request.Occupation, request.Personality);

            var prompt = $@"Create a sci-fi NPC (non-player character) for a tabletop RPG:
Species: {request.Species}
Occupation: {request.Occupation}
Personality: {request.Personality}
Setting: {request.Setting}

Respond with a JSON object containing:
- name: A unique sci-fi name fitting their species
- occupation: Their job/role description
- personality: A brief personality summary
- background: A 2-3 sentence backstory explaining who they are and their motivations
- stats: An object with CHA (charisma, 1-100), INT (intelligence, 1-100), WIS (wisdom, 1-100)

Example format:
{{""name"":""Vex Morrow"",""occupation"":""Information Broker"",""personality"":""Cunning but fair"",""background"":""Former corporate spy..."",""stats"":{{""CHA"":85,""INT"":75,""WIS"":60}}}}";

            var textResult = await _aiService.GenerateJsonAsync(
                prompt,
                "You are a sci-fi RPG character creator. Create interesting NPCs with depth. Respond only with valid JSON.",
                0.8f,
                512,
                cancellationToken);

            // Generate image for the NPC
            var imagePrompt = $"High-quality futuristic sci-fi portrait of a {request.Species} {request.Occupation}, {request.Personality} expression. Cinematic lighting, cyberpunk aesthetic, detailed face, professional concept art, neutral background, 8k resolution.";
            
            var imageResult = await _aiService.GenerateImageAsync(imagePrompt, cancellationToken: cancellationToken);

            return Ok(new NpcGenerationResponse
            {
                Success = true,
                NpcJson = textResult.Json,
                ImageBase64 = imageResult.ImageBase64,
                ImageUrl = imageResult.ImageUrl
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate NPC");
            return StatusCode(500, new NpcGenerationResponse
            {
                Success = false,
                Error = ex.Message
            });
        }
    }

    /// <summary>
    /// Generate an enemy/hostile creature with combat stats
    /// </summary>
    [HttpPost("generate/enemy")]
    [ProducesResponseType(typeof(EnemyGenerationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<EnemyGenerationResponse>> GenerateEnemy(
        [FromBody] EnemyGenerationRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Generating enemy: Species={Species}, ThreatLevel={ThreatLevel}, Behavior={Behavior}", 
                request.Species, request.ThreatLevel, request.Behavior);

            var prompt = $@"Create a hostile sci-fi creature/enemy for a tabletop RPG:
Species Type: {request.Species}
Threat Level: {request.ThreatLevel}
Behavior Pattern: {request.Behavior}
Environment: {request.Environment}

Respond with a JSON object containing:
- name: A threatening designation or creature name
- species: The creature type/classification
- threatLevel: The danger level ({request.ThreatLevel})
- abilities: A description of 2-3 special abilities or attacks
- weakness: One exploitable vulnerability
- stats: An object with HP (health, 50-500 based on threat), ATK (attack power, 1-100), DEF (defense, 1-100), SPD (speed, 1-100)

Example format:
{{""name"":""Void Stalker"",""species"":""Alien Predator"",""threatLevel"":""dangerous"",""abilities"":""Cloaking field, venomous claws..."",""weakness"":""Sensitive to bright light"",""stats"":{{""HP"":150,""ATK"":75,""DEF"":40,""SPD"":90}}}}";

            var textResult = await _aiService.GenerateJsonAsync(
                prompt,
                "You are a sci-fi monster designer. Create terrifying but balanced enemies. Respond only with valid JSON.",
                0.8f,
                512,
                cancellationToken);

            // Generate image for the enemy
            var imagePrompt = $"Terrifying sci-fi creature concept art, {request.Species}, {request.Behavior} posture, menacing, dark atmosphere, highly detailed, horror sci-fi aesthetic, professional illustration, dramatic lighting, 8k resolution.";
            
            var imageResult = await _aiService.GenerateImageAsync(imagePrompt, cancellationToken: cancellationToken);

            return Ok(new EnemyGenerationResponse
            {
                Success = true,
                EnemyJson = textResult.Json,
                ImageBase64 = imageResult.ImageBase64,
                ImageUrl = imageResult.ImageUrl
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate enemy");
            return StatusCode(500, new EnemyGenerationResponse
            {
                Success = false,
                Error = ex.Message
            });
        }
    }

    /// <summary>
    /// Generate a mission/quest with objectives and rewards
    /// </summary>
    [HttpPost("generate/mission")]
    [ProducesResponseType(typeof(MissionGenerationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<MissionGenerationResponse>> GenerateMission(
        [FromBody] MissionGenerationRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Generating mission: Type={MissionType}, Difficulty={Difficulty}", 
                request.MissionType, request.Difficulty);

            var prompt = $@"Create a sci-fi RPG mission/quest:
Mission Type: {request.MissionType}
Difficulty: {request.Difficulty}
Environment: {request.Environment}
Faction Involved: {request.FactionInvolved}

Respond with a JSON object containing:
- name: A code name or operation title (e.g., ""Operation Silent Dawn"")
- briefing: A 2-3 sentence mission briefing explaining the situation
- objective: The primary goal in one clear sentence
- rewards: Expected compensation/rewards for completion
- difficulty: The difficulty level ({request.Difficulty})
- estimatedDuration: Approximate time to complete (e.g., ""2-3 hours"")

Example format:
{{""name"":""Operation Blackout"",""briefing"":""Corporate forces have seized..."",""objective"":""Infiltrate the facility and retrieve the data core"",""rewards"":""5000 credits + reputation boost"",""difficulty"":""HARD"",""estimatedDuration"":""3-4 hours""}}";

            var textResult = await _aiService.GenerateJsonAsync(
                prompt,
                "You are a sci-fi mission designer. Create engaging quests with clear objectives. Respond only with valid JSON.",
                0.8f,
                512,
                cancellationToken);

            return Ok(new MissionGenerationResponse
            {
                Success = true,
                MissionJson = textResult.Json
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate mission");
            return StatusCode(500, new MissionGenerationResponse
            {
                Success = false,
                Error = ex.Message
            });
        }
    }

    /// <summary>
    /// Generate a combat encounter with participants and environment
    /// </summary>
    [HttpPost("generate/encounter")]
    [ProducesResponseType(typeof(EncounterGenerationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<EncounterGenerationResponse>> GenerateEncounter(
        [FromBody] EncounterGenerationRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Generating encounter: Type={EncounterType}, Difficulty={Difficulty}, EnemyCount={EnemyCount}", 
                request.EncounterType, request.Difficulty, request.EnemyCount);

            var prompt = $@"Create a sci-fi RPG combat/tactical encounter:
Encounter Type: {request.EncounterType}
Difficulty: {request.Difficulty}
Environment: {request.Environment}
Enemy Count: {request.EnemyCount}

Respond with a JSON object containing:
- name: An evocative encounter name (e.g., ""Ambush at Sector 7"")
- description: A 2-3 sentence description of the situation and how the encounter begins
- environment: Detailed description of the tactical environment and any hazards
- participants: An array of enemy types/names involved in this encounter
- difficulty: The challenge level ({request.Difficulty})
- loot: Potential rewards if the encounter is won

Example format:
{{""name"":""Cargo Bay Showdown"",""description"":""The party enters the cargo bay to find..."",""environment"":""Large open space with shipping containers providing cover"",""participants"":[""Security Droid x2"",""Corporate Enforcer""],""difficulty"":""MEDIUM"",""loot"":""Prototype weapon, 1200 credits""}}";

            var textResult = await _aiService.GenerateJsonAsync(
                prompt,
                "You are a sci-fi encounter designer. Create tactical and exciting combat scenarios. Respond only with valid JSON.",
                0.8f,
                512,
                cancellationToken);

            return Ok(new EncounterGenerationResponse
            {
                Success = true,
                EncounterJson = textResult.Json
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate encounter");
            return StatusCode(500, new EncounterGenerationResponse
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

// NPC Generation DTOs
public record NpcGenerationRequest
{
    public string Species { get; init; } = "human";
    public string Occupation { get; init; } = "merchant";
    public string Personality { get; init; } = "friendly";
    public string Setting { get; init; } = "space-station";
}

public record NpcGenerationResponse
{
    public bool Success { get; init; }
    public string? NpcJson { get; init; }
    public string? ImageBase64 { get; init; }
    public string? ImageUrl { get; init; }
    public string? Error { get; init; }
}

// Enemy Generation DTOs
public record EnemyGenerationRequest
{
    public string Species { get; init; } = "alien-beast";
    public string ThreatLevel { get; init; } = "moderate";
    public string Behavior { get; init; } = "aggressive";
    public string Environment { get; init; } = "space-station";
}

public record EnemyGenerationResponse
{
    public bool Success { get; init; }
    public string? EnemyJson { get; init; }
    public string? ImageBase64 { get; init; }
    public string? ImageUrl { get; init; }
    public string? Error { get; init; }
}

// Mission Generation DTOs
public record MissionGenerationRequest
{
    public string MissionType { get; init; } = "extraction";
    public string Difficulty { get; init; } = "MEDIUM";
    public string Environment { get; init; } = "space-station";
    public string FactionInvolved { get; init; } = "corporate";
}

public record MissionGenerationResponse
{
    public bool Success { get; init; }
    public string? MissionJson { get; init; }
    public string? Error { get; init; }
}

// Encounter Generation DTOs
public record EncounterGenerationRequest
{
    public string EncounterType { get; init; } = "combat";
    public string Difficulty { get; init; } = "MEDIUM";
    public string Environment { get; init; } = "open-area";
    public string EnemyCount { get; init; } = "squad";
}

public record EncounterGenerationResponse
{
    public bool Success { get; init; }
    public string? EncounterJson { get; init; }
    public string? Error { get; init; }
}
