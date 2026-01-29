using Loremaster.Application.Common.Interfaces;
using Loremaster.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Loremaster.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class GameSystemsController : ControllerBase
{
    private readonly IGameSystemRepository _gameSystemRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<GameSystemsController> _logger;

    public GameSystemsController(
        IGameSystemRepository gameSystemRepository,
        ICurrentUserService currentUserService,
        IUnitOfWork unitOfWork,
        ILogger<GameSystemsController> logger)
    {
        _gameSystemRepository = gameSystemRepository;
        _currentUserService = currentUserService;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    /// <summary>
    /// Get all active game systems (public endpoint)
    /// </summary>
    [HttpGet]
    [AllowAnonymous]
    [ProducesResponseType(typeof(IEnumerable<GameSystemDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<GameSystemDto>>> GetAll(CancellationToken cancellationToken)
    {
        var gameSystems = await _gameSystemRepository.GetActiveAsync(cancellationToken);
        
        var dtos = gameSystems.Select(gs => new GameSystemDto
        {
            Id = gs.Id,
            Code = gs.Code,
            Name = gs.Name,
            Publisher = gs.Publisher,
            Version = gs.Version,
            Description = gs.Description,
            SupportedEntityTypes = gs.SupportedEntityTypes
        });

        return Ok(dtos);
    }

    /// <summary>
    /// Get a game system by ID
    /// </summary>
    [HttpGet("{id:guid}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(GameSystemDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<GameSystemDto>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var gameSystem = await _gameSystemRepository.GetByIdAsync(id, cancellationToken);
        if (gameSystem == null)
            return NotFound();

        return Ok(new GameSystemDto
        {
            Id = gameSystem.Id,
            Code = gameSystem.Code,
            Name = gameSystem.Name,
            Publisher = gameSystem.Publisher,
            Version = gameSystem.Version,
            Description = gameSystem.Description,
            SupportedEntityTypes = gameSystem.SupportedEntityTypes
        });
    }

    /// <summary>
    /// Get a game system by code
    /// </summary>
    [HttpGet("by-code/{code}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(GameSystemDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<GameSystemDto>> GetByCode(string code, CancellationToken cancellationToken)
    {
        var gameSystem = await _gameSystemRepository.GetByCodeAsync(code, cancellationToken);
        if (gameSystem == null)
            return NotFound();

        return Ok(new GameSystemDto
        {
            Id = gameSystem.Id,
            Code = gameSystem.Code,
            Name = gameSystem.Name,
            Publisher = gameSystem.Publisher,
            Version = gameSystem.Version,
            Description = gameSystem.Description,
            SupportedEntityTypes = gameSystem.SupportedEntityTypes
        });
    }

    /// <summary>
    /// Create a new game system (Admin only)
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(GameSystemDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<GameSystemDto>> Create(
        [FromBody] CreateGameSystemRequest request,
        CancellationToken cancellationToken)
    {
        // Check if code already exists
        var exists = await _gameSystemRepository.ExistsByCodeAsync(request.Code, cancellationToken);
        if (exists)
            return BadRequest("A game system with this code already exists");

        var gameSystem = GameSystem.Create(
            code: request.Code,
            name: request.Name,
            publisher: request.Publisher,
            version: request.Version,
            description: request.Description,
            supportedEntityTypes: request.SupportedEntityTypes
        );

        await _gameSystemRepository.AddAsync(gameSystem, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("GameSystem {Code} created by user {UserId}", 
            gameSystem.Code, _currentUserService.UserId);

        return CreatedAtAction(
            nameof(GetById),
            new { id = gameSystem.Id },
            new GameSystemDto
            {
                Id = gameSystem.Id,
                Code = gameSystem.Code,
                Name = gameSystem.Name,
                Publisher = gameSystem.Publisher,
                Version = gameSystem.Version,
                Description = gameSystem.Description,
                SupportedEntityTypes = gameSystem.SupportedEntityTypes
            });
    }

    /// <summary>
    /// Update a game system (Admin only)
    /// </summary>
    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(GameSystemDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<GameSystemDto>> Update(
        Guid id,
        [FromBody] UpdateGameSystemRequest request,
        CancellationToken cancellationToken)
    {
        var gameSystem = await _gameSystemRepository.GetByIdAsync(id, cancellationToken);
        if (gameSystem == null)
            return NotFound();

        gameSystem.Update(
            name: request.Name,
            publisher: request.Publisher,
            version: request.Version,
            description: request.Description,
            supportedEntityTypes: request.SupportedEntityTypes
        );

        _gameSystemRepository.Update(gameSystem);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("GameSystem {Code} updated by user {UserId}", 
            gameSystem.Code, _currentUserService.UserId);

        return Ok(new GameSystemDto
        {
            Id = gameSystem.Id,
            Code = gameSystem.Code,
            Name = gameSystem.Name,
            Publisher = gameSystem.Publisher,
            Version = gameSystem.Version,
            Description = gameSystem.Description,
            SupportedEntityTypes = gameSystem.SupportedEntityTypes
        });
    }

    /// <summary>
    /// Activate/Deactivate a game system (Admin only)
    /// </summary>
    [HttpPatch("{id:guid}/status")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(GameSystemDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<GameSystemDto>> UpdateStatus(
        Guid id,
        [FromBody] UpdateGameSystemStatusRequest request,
        CancellationToken cancellationToken)
    {
        var gameSystem = await _gameSystemRepository.GetByIdAsync(id, cancellationToken);
        if (gameSystem == null)
            return NotFound();

        if (request.IsActive)
            gameSystem.Activate();
        else
            gameSystem.Deactivate();

        _gameSystemRepository.Update(gameSystem);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("GameSystem {Code} status changed to {IsActive} by user {UserId}", 
            gameSystem.Code, request.IsActive, _currentUserService.UserId);

        return Ok(new GameSystemDto
        {
            Id = gameSystem.Id,
            Code = gameSystem.Code,
            Name = gameSystem.Name,
            Publisher = gameSystem.Publisher,
            Version = gameSystem.Version,
            Description = gameSystem.Description,
            SupportedEntityTypes = gameSystem.SupportedEntityTypes
        });
    }
}

// DTOs
public record GameSystemDto
{
    public Guid Id { get; init; }
    public string Code { get; init; } = null!;
    public string Name { get; init; } = null!;
    public string? Publisher { get; init; }
    public string? Version { get; init; }
    public string? Description { get; init; }
    public List<string> SupportedEntityTypes { get; init; } = new();
}

public record CreateGameSystemRequest
{
    public string Code { get; init; } = null!;
    public string Name { get; init; } = null!;
    public string? Publisher { get; init; }
    public string? Version { get; init; }
    public string? Description { get; init; }
    public List<string>? SupportedEntityTypes { get; init; }
}

public record UpdateGameSystemRequest
{
    public string Name { get; init; } = null!;
    public string? Publisher { get; init; }
    public string? Version { get; init; }
    public string? Description { get; init; }
    public List<string>? SupportedEntityTypes { get; init; }
}

public record UpdateGameSystemStatusRequest
{
    public bool IsActive { get; init; }
}
