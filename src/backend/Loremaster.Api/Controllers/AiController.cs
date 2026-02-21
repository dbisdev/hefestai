using System.Security.Claims;
using Loremaster.Application.Common.Interfaces;
using Loremaster.Application.Features.EntityGeneration.Commands.GenerateCharacter;
using Loremaster.Application.Features.EntityGeneration.Commands.GenerateSolarSystem;
using Loremaster.Application.Features.EntityGeneration.Commands.GenerateVehicle;
using Loremaster.Application.Features.EntityGeneration.Commands.GenerateNpc;
using Loremaster.Application.Features.EntityGeneration.Commands.GenerateEnemy;
using Loremaster.Application.Features.EntityGeneration.Commands.GenerateMission;
using Loremaster.Application.Features.EntityGeneration.Commands.GenerateEncounter;
using Loremaster.Application.Features.EntityGeneration.Commands.Shared;
using Loremaster.Application.Features.EntityGeneration.DTOs;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Loremaster.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class AiController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly IAiService _aiService;
    private readonly ILogger<AiController> _logger;

    public AiController(
        IMediator mediator,
        IAiService aiService,
        ILogger<AiController> logger)
    {
        _mediator = mediator;
        _aiService = aiService;
        _logger = logger;
    }

    private Guid GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst("sub")?.Value
            ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            throw new UnauthorizedAccessException("Invalid user token");

        return userId;
    }

    #region Entity Generation Endpoints

    [HttpPost("generate/character")]
    [ProducesResponseType(typeof(CharacterGenerationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<CharacterGenerationResponse>> GenerateCharacter(
        [FromBody] CharacterGenerationRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var command = new GenerateCharacterCommand
            {
                UserId = GetCurrentUserId(),
                GameSystemId = request.GameSystemId,
                Species = request.Species,
                Role = request.Role,
                Morphology = request.Morphology,
                Attire = request.Attire,
                GenerateImage = request.GenerateImage
            };

            var result = await _mediator.Send(command, cancellationToken);

            return Ok(new CharacterGenerationResponse
            {
                Success = result.Success,
                CharacterJson = result.EntityJson,
                ImageBase64 = result.ImageBase64,
                ImageUrl = result.ImageUrl,
                RagContextUsed = result.RagContextUsed,
                RagSourceCount = result.RagSourceCount,
                GenerationRequestId = result.GenerationRequestId,
                Error = result.Error
            });
        }
        catch (UnauthorizedAccessException)
        {
            return Unauthorized();
        }
    }

    [HttpPost("generate/solar-system")]
    [ProducesResponseType(typeof(SolarSystemGenerationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<SolarSystemGenerationResponse>> GenerateSolarSystem(
        [FromBody] SolarSystemGenerationRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var command = new GenerateSolarSystemCommand
            {
                UserId = GetCurrentUserId(),
                GameSystemId = request.GameSystemId,
                SpectralClass = request.SpectralClass,
                PlanetCount = request.PlanetCount,
                GenerateImage = request.GenerateImage
            };

            var result = await _mediator.Send(command, cancellationToken);

            return Ok(new SolarSystemGenerationResponse
            {
                Success = result.Success,
                SystemJson = result.EntityJson,
                ImageBase64 = result.ImageBase64,
                ImageUrl = result.ImageUrl,
                RagContextUsed = result.RagContextUsed,
                RagSourceCount = result.RagSourceCount,
                GenerationRequestId = result.GenerationRequestId,
                Error = result.Error
            });
        }
        catch (UnauthorizedAccessException)
        {
            return Unauthorized();
        }
    }

    [HttpPost("generate/vehicle")]
    [ProducesResponseType(typeof(VehicleGenerationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<VehicleGenerationResponse>> GenerateVehicle(
        [FromBody] VehicleGenerationRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var command = new GenerateVehicleCommand
            {
                UserId = GetCurrentUserId(),
                GameSystemId = request.GameSystemId,
                Type = request.Type,
                Class = request.Class,
                Engine = request.Engine,
                GenerateImage = request.GenerateImage
            };

            var result = await _mediator.Send(command, cancellationToken);

            return Ok(new VehicleGenerationResponse
            {
                Success = result.Success,
                VehicleJson = result.EntityJson,
                ImageBase64 = result.ImageBase64,
                ImageUrl = result.ImageUrl,
                RagContextUsed = result.RagContextUsed,
                RagSourceCount = result.RagSourceCount,
                GenerationRequestId = result.GenerationRequestId,
                Error = result.Error
            });
        }
        catch (UnauthorizedAccessException)
        {
            return Unauthorized();
        }
    }

    [HttpPost("generate/npc")]
    [ProducesResponseType(typeof(NpcGenerationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<NpcGenerationResponse>> GenerateNpc(
        [FromBody] NpcGenerationRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var command = new GenerateNpcCommand
            {
                UserId = GetCurrentUserId(),
                GameSystemId = request.GameSystemId,
                Species = request.Species,
                Occupation = request.Occupation,
                Personality = request.Personality,
                Setting = request.Setting,
                GenerateImage = request.GenerateImage
            };

            var result = await _mediator.Send(command, cancellationToken);

            return Ok(new NpcGenerationResponse
            {
                Success = result.Success,
                NpcJson = result.EntityJson,
                ImageBase64 = result.ImageBase64,
                ImageUrl = result.ImageUrl,
                RagContextUsed = result.RagContextUsed,
                RagSourceCount = result.RagSourceCount,
                GenerationRequestId = result.GenerationRequestId,
                Error = result.Error
            });
        }
        catch (UnauthorizedAccessException)
        {
            return Unauthorized();
        }
    }

    [HttpPost("generate/enemy")]
    [ProducesResponseType(typeof(EnemyGenerationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<EnemyGenerationResponse>> GenerateEnemy(
        [FromBody] EnemyGenerationRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var command = new GenerateEnemyCommand
            {
                UserId = GetCurrentUserId(),
                GameSystemId = request.GameSystemId,
                Species = request.Species,
                ThreatLevel = request.ThreatLevel,
                Behavior = request.Behavior,
                Environment = request.Environment,
                GenerateImage = request.GenerateImage
            };

            var result = await _mediator.Send(command, cancellationToken);

            return Ok(new EnemyGenerationResponse
            {
                Success = result.Success,
                EnemyJson = result.EntityJson,
                ImageBase64 = result.ImageBase64,
                ImageUrl = result.ImageUrl,
                RagContextUsed = result.RagContextUsed,
                RagSourceCount = result.RagSourceCount,
                GenerationRequestId = result.GenerationRequestId,
                Error = result.Error
            });
        }
        catch (UnauthorizedAccessException)
        {
            return Unauthorized();
        }
    }

    [HttpPost("generate/mission")]
    [ProducesResponseType(typeof(MissionGenerationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<MissionGenerationResponse>> GenerateMission(
        [FromBody] MissionGenerationRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var command = new GenerateMissionCommand
            {
                UserId = GetCurrentUserId(),
                GameSystemId = request.GameSystemId,
                MissionType = request.MissionType,
                Difficulty = request.Difficulty,
                Environment = request.Environment,
                FactionInvolved = request.FactionInvolved,
                GenerateImage = request.GenerateImage
            };

            var result = await _mediator.Send(command, cancellationToken);

            return Ok(new MissionGenerationResponse
            {
                Success = result.Success,
                MissionJson = result.EntityJson,
                ImageBase64 = result.ImageBase64,
                ImageUrl = result.ImageUrl,
                RagContextUsed = result.RagContextUsed,
                RagSourceCount = result.RagSourceCount,
                GenerationRequestId = result.GenerationRequestId,
                Error = result.Error
            });
        }
        catch (UnauthorizedAccessException)
        {
            return Unauthorized();
        }
    }

    [HttpPost("generate/encounter")]
    [ProducesResponseType(typeof(EncounterGenerationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<EncounterGenerationResponse>> GenerateEncounter(
        [FromBody] EncounterGenerationRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var command = new GenerateEncounterCommand
            {
                UserId = GetCurrentUserId(),
                GameSystemId = request.GameSystemId,
                EncounterType = request.EncounterType,
                Difficulty = request.Difficulty,
                Environment = request.Environment,
                EnemyCount = request.EnemyCount,
                GenerateImage = request.GenerateImage
            };

            var result = await _mediator.Send(command, cancellationToken);

            return Ok(new EncounterGenerationResponse
            {
                Success = result.Success,
                EncounterJson = result.EntityJson,
                ImageBase64 = result.ImageBase64,
                ImageUrl = result.ImageUrl,
                RagContextUsed = result.RagContextUsed,
                RagSourceCount = result.RagSourceCount,
                GenerationRequestId = result.GenerationRequestId,
                Error = result.Error
            });
        }
        catch (UnauthorizedAccessException)
        {
            return Unauthorized();
        }
    }

    #endregion

    #region Utility Endpoints

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

    #endregion
}

#region Request/Response DTOs

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

#endregion
