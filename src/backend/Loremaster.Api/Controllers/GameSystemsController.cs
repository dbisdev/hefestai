using Loremaster.Application.Features.GameSystems.Commands.CreateGameSystem;
using Loremaster.Application.Features.GameSystems.Commands.UpdateGameSystem;
using Loremaster.Application.Features.GameSystems.Commands.UpdateGameSystemStatus;
using Loremaster.Application.Features.GameSystems.DTOs;
using Loremaster.Application.Features.GameSystems.Queries.GetAllGameSystems;
using Loremaster.Application.Features.GameSystems.Queries.GetGameSystemByCode;
using Loremaster.Application.Features.GameSystems.Queries.GetGameSystemById;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Loremaster.Api.Controllers;

/// <summary>
/// Controller for managing game systems (tabletop RPG rule sets).
/// Most endpoints are public; create/update/status require Master or Admin role.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class GameSystemsController : ControllerBase
{
    private readonly IMediator _mediator;

    public GameSystemsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Get all active game systems (public endpoint).
    /// </summary>
    [HttpGet]
    [AllowAnonymous]
    [ProducesResponseType(typeof(IEnumerable<GameSystemDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<GameSystemDto>>> GetAll(CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetAllGameSystemsQuery(), cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Get a game system by ID (public endpoint).
    /// </summary>
    [HttpGet("{id:guid}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(GameSystemDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<GameSystemDto>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetGameSystemByIdQuery(id), cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Get a game system by code (public endpoint).
    /// </summary>
    [HttpGet("by-code/{code}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(GameSystemDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<GameSystemDto>> GetByCode(string code, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetGameSystemByCodeQuery(code), cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Create a new game system (Master or Admin only).
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "Master,Admin")]
    [ProducesResponseType(typeof(GameSystemDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<GameSystemDto>> Create(
        [FromBody] CreateGameSystemRequest request,
        CancellationToken cancellationToken)
    {
        var command = new CreateGameSystemCommand(
            Code: request.Code,
            Name: request.Name,
            Publisher: request.Publisher,
            Version: request.Version,
            Description: request.Description,
            SupportedEntityTypes: request.SupportedEntityTypes
        );
        
        var result = await _mediator.Send(command, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    /// <summary>
    /// Update a game system (Master or Admin only).
    /// </summary>
    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Master,Admin")]
    [ProducesResponseType(typeof(GameSystemDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<GameSystemDto>> Update(
        Guid id,
        [FromBody] UpdateGameSystemRequest request,
        CancellationToken cancellationToken)
    {
        var command = new UpdateGameSystemCommand(
            Id: id,
            Name: request.Name,
            Publisher: request.Publisher,
            Version: request.Version,
            Description: request.Description,
            SupportedEntityTypes: request.SupportedEntityTypes
        );
        
        var result = await _mediator.Send(command, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Activate/Deactivate a game system (Master or Admin only).
    /// </summary>
    [HttpPatch("{id:guid}/status")]
    [Authorize(Roles = "Master,Admin")]
    [ProducesResponseType(typeof(GameSystemDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<GameSystemDto>> UpdateStatus(
        Guid id,
        [FromBody] UpdateGameSystemStatusRequest request,
        CancellationToken cancellationToken)
    {
        var command = new UpdateGameSystemStatusCommand(id, request.IsActive);
        var result = await _mediator.Send(command, cancellationToken);
        return Ok(result);
    }
}

#region Request DTOs

/// <summary>
/// Request to create a new game system.
/// </summary>
public record CreateGameSystemRequest
{
    public string Code { get; init; } = null!;
    public string Name { get; init; } = null!;
    public string? Publisher { get; init; }
    public string? Version { get; init; }
    public string? Description { get; init; }
    public List<string>? SupportedEntityTypes { get; init; }
}

/// <summary>
/// Request to update a game system.
/// </summary>
public record UpdateGameSystemRequest
{
    public string Name { get; init; } = null!;
    public string? Publisher { get; init; }
    public string? Version { get; init; }
    public string? Description { get; init; }
    public List<string>? SupportedEntityTypes { get; init; }
}

/// <summary>
/// Request to update game system status.
/// </summary>
public record UpdateGameSystemStatusRequest
{
    public bool IsActive { get; init; }
}

#endregion
