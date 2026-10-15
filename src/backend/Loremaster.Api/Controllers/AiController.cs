using Loremaster.Application.Common.Models;
using Loremaster.Application.Features.Ai.Queries.CheckAiHealth;
using Loremaster.Application.Features.EntityGeneration.Commands.GenerateCharacter;
using Loremaster.Application.Features.EntityGeneration.Commands.GenerateSolarSystem;
using Loremaster.Application.Features.EntityGeneration.Commands.GenerateVehicle;
using Loremaster.Application.Features.EntityGeneration.Commands.GenerateNpc;
using Loremaster.Application.Features.EntityGeneration.Commands.GenerateEnemy;
using Loremaster.Application.Features.EntityGeneration.Commands.GenerateMission;
using Loremaster.Application.Features.EntityGeneration.Commands.GenerateEncounter;
using Loremaster.Application.Features.EntityGeneration.Commands.TestAi;
using Loremaster.Application.Features.EntityGeneration.Commands.Shared;
using Loremaster.Application.Features.EntityGeneration.DTOs;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Loremaster.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class AiController : ApiControllerBase
{
    private readonly IMediator _mediator;

    public AiController(IMediator mediator)
    {
        _mediator = mediator;
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
                EnemyCount = request.EnemyCount.ToString().ToLowerInvariant(),
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

    /// <summary>
    /// Tests AI service connectivity by sending a prompt and returning the generated text.
    /// Errors are captured inside <see cref="TestAiResult"/> rather than thrown, so the
    /// endpoint always returns 200 with a success/error payload.
    /// </summary>
    [HttpPost("test")]
    [ProducesResponseType(typeof(TestAiResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<TestAiResponse>> TestGenkit(
        [FromBody] TestAiRequest request,
        CancellationToken cancellationToken)
    {
        var command = new TestAiCommand(
            Prompt: request.Prompt,
            SystemPrompt: request.SystemPrompt,
            Temperature: request.Temperature ?? 0.7f,
            MaxTokens: request.MaxTokens ?? 256);

        var result = await _mediator.Send(command, cancellationToken);

        return Ok(new TestAiResponse
        {
            Success = result.Success,
            Text = result.Text,
            Error = result.Error,
            Usage = result.Usage is not null
                ? new TokenUsageResponse
                {
                    PromptTokens = result.Usage.PromptTokens,
                    CompletionTokens = result.Usage.CompletionTokens,
                    TotalTokens = result.Usage.TotalTokens
                }
                : null
        });
    }

    /// <summary>
    /// Health-check endpoint for the AI service. Anonymous access allowed.
    /// </summary>
    [HttpGet("health")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(HealthResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<HealthResponse>> CheckHealth(CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new CheckAiHealthQuery(), cancellationToken);

        return Ok(new HealthResponse
        {
            IsHealthy = result.IsHealthy,
            Service = result.Service,
            Timestamp = result.Timestamp
        });
    }

    #endregion
}

#region API-layer DTOs

/// <summary>Response DTO for the <c>GET /api/ai/health</c> endpoint.</summary>
public record HealthResponse
{
    /// <summary>Whether the AI service responded successfully.</summary>
    public bool IsHealthy { get; init; }

    /// <summary>Human-readable service identifier.</summary>
    public string Service { get; init; } = null!;

    /// <summary>UTC timestamp of the health check.</summary>
    public DateTime Timestamp { get; init; }
}

#endregion
