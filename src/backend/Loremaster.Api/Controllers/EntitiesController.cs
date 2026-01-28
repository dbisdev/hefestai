using Loremaster.Application.Common.Interfaces;
using Loremaster.Domain.Entities;
using Loremaster.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Loremaster.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class EntitiesController : ControllerBase
{
    private readonly IWorldEntityRepository _entityRepository;
    private readonly IUserRepository _userRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<EntitiesController> _logger;

    public EntitiesController(
        IWorldEntityRepository entityRepository,
        IUserRepository userRepository,
        ICurrentUserService currentUserService,
        IUnitOfWork unitOfWork,
        ILogger<EntitiesController> logger)
    {
        _entityRepository = entityRepository;
        _userRepository = userRepository;
        _currentUserService = currentUserService;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    /// <summary>
    /// Get all visible entities for the current user
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<EntityDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<EntityDto>>> GetVisibleEntities(
        [FromQuery] string? category,
        CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId;
        if (userId == null)
            return Unauthorized();

        var user = await _userRepository.GetByIdAsync(userId.Value, cancellationToken);
        if (user == null)
            return Unauthorized();

        var entities = await _entityRepository.GetVisibleEntitiesAsync(
            userId.Value, 
            user.MasterId, 
            cancellationToken);

        // Filter by category if provided
        if (!string.IsNullOrEmpty(category) && Enum.TryParse<EntityCategory>(category, true, out var cat))
        {
            entities = entities.Where(e => e.Category == cat);
        }

        var dtos = entities.Select(e => MapToDto(e));
        return Ok(dtos);
    }

    /// <summary>
    /// Get an entity by ID
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(EntityDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<EntityDto>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId;
        if (userId == null)
            return Unauthorized();

        var user = await _userRepository.GetByIdAsync(userId.Value, cancellationToken);
        if (user == null)
            return Unauthorized();

        var entity = await _entityRepository.GetByIdAsync(id, cancellationToken);
        if (entity == null)
            return NotFound();

        // Check access: Masters can only see their own entities, Players can see their Master's
        var allowedCreatorId = user.IsMaster ? userId.Value : user.MasterId;
        if (entity.CreatorId != allowedCreatorId)
            return Forbid();

        return Ok(MapToDto(entity));
    }

    /// <summary>
    /// Create a new entity (Masters only)
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(EntityDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<EntityDto>> Create(
        [FromBody] CreateEntityRequest request,
        CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId;
        if (userId == null)
            return Unauthorized();

        var user = await _userRepository.GetByIdAsync(userId.Value, cancellationToken);
        if (user == null)
            return Unauthorized();

        // Only Masters can create entities
        if (!user.IsMaster && !user.IsAdmin)
            return Forbid("Only Masters can create entities");

        if (!Enum.TryParse<EntityCategory>(request.Category, true, out var category))
            return BadRequest("Invalid category");

        var entity = WorldEntity.Create(
            request.Name,
            request.Type,
            request.Meta,
            request.Image,
            category,
            userId.Value,
            request.Description,
            request.Stats
        );

        await _entityRepository.AddAsync(entity, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Entity {EntityId} created by user {UserId}", entity.Id, userId);

        return CreatedAtAction(
            nameof(GetById), 
            new { id = entity.Id }, 
            MapToDto(entity));
    }

    /// <summary>
    /// Update an entity (Masters only, own entities)
    /// </summary>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(EntityDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<EntityDto>> Update(
        Guid id,
        [FromBody] UpdateEntityRequest request,
        CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId;
        if (userId == null)
            return Unauthorized();

        var user = await _userRepository.GetByIdAsync(userId.Value, cancellationToken);
        if (user == null)
            return Unauthorized();

        var entity = await _entityRepository.GetByIdAsync(id, cancellationToken);
        if (entity == null)
            return NotFound();

        // Only the creator (Master) can update
        if (entity.CreatorId != userId.Value)
            return Forbid("Only the creator can update this entity");

        entity.UpdateDetails(request.Name, request.Type, request.Meta, request.Description);
        
        if (request.Image != null)
            entity.UpdateImage(request.Image);
            
        if (request.Stats != null)
            entity.UpdateStats(request.Stats);

        _entityRepository.Update(entity);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Ok(MapToDto(entity));
    }

    /// <summary>
    /// Delete an entity (Masters only, own entities)
    /// </summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId;
        if (userId == null)
            return Unauthorized();

        var entity = await _entityRepository.GetByIdAsync(id, cancellationToken);
        if (entity == null)
            return NotFound();

        // Only the creator (Master) can delete
        if (entity.CreatorId != userId.Value)
            return Forbid("Only the creator can delete this entity");

        _entityRepository.Delete(entity);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Entity {EntityId} deleted by user {UserId}", id, userId);

        return NoContent();
    }

    private static EntityDto MapToDto(WorldEntity entity)
    {
        return new EntityDto
        {
            Id = entity.Id.ToString(),
            Name = entity.Name,
            Type = entity.Type,
            Meta = entity.Meta,
            Image = entity.Image,
            Category = entity.Category.ToString().ToUpperInvariant(),
            Description = entity.Description,
            Stats = entity.GetStats(),
            CreatorId = entity.CreatorId.ToString(),
            CreatedAt = entity.CreatedAt
        };
    }
}

// DTOs
public record EntityDto
{
    public string Id { get; init; } = null!;
    public string Name { get; init; } = null!;
    public string Type { get; init; } = null!;
    public string Meta { get; init; } = null!;
    public string Image { get; init; } = null!;
    public string Category { get; init; } = null!;
    public string? Description { get; init; }
    public Dictionary<string, object>? Stats { get; init; }
    public string CreatorId { get; init; } = null!;
    public DateTime CreatedAt { get; init; }
}

public record CreateEntityRequest
{
    public string Name { get; init; } = null!;
    public string Type { get; init; } = null!;
    public string Meta { get; init; } = null!;
    public string Image { get; init; } = null!;
    public string Category { get; init; } = null!;
    public string? Description { get; init; }
    public Dictionary<string, object>? Stats { get; init; }
}

public record UpdateEntityRequest
{
    public string Name { get; init; } = null!;
    public string Type { get; init; } = null!;
    public string Meta { get; init; } = null!;
    public string? Image { get; init; }
    public string? Description { get; init; }
    public Dictionary<string, object>? Stats { get; init; }
}
