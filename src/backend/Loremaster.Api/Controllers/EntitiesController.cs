using System.Text.Json;
using Loremaster.Application.Common.Interfaces;
using Loremaster.Domain.Entities;
using Loremaster.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Loremaster.Api.Controllers;

[ApiController]
[Route("api/campaigns/{campaignId:guid}/[controller]")]
[Authorize]
public class EntitiesController : ControllerBase
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ICurrentUserService _currentUserService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<EntitiesController> _logger;

    public EntitiesController(
        IApplicationDbContext dbContext,
        ICurrentUserService currentUserService,
        IUnitOfWork unitOfWork,
        ILogger<EntitiesController> logger)
    {
        _dbContext = dbContext;
        _currentUserService = currentUserService;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    /// <summary>
    /// Get all visible entities in a campaign for the current user
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<LoreEntityDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IEnumerable<LoreEntityDto>>> GetCampaignEntities(
        Guid campaignId,
        [FromQuery] string? entityType,
        [FromQuery] VisibilityLevel? visibility,
        CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId;
        if (userId == null)
            return Unauthorized();

        // Check campaign membership
        var membership = await GetCampaignMembershipAsync(campaignId, userId.Value, cancellationToken);
        if (membership == null)
            return NotFound("Campaign not found or you are not a member");

        var query = _dbContext.LoreEntities
            .Where(e => e.CampaignId == campaignId && e.DeletedAt == null);

        // Filter by entity type if provided
        if (!string.IsNullOrEmpty(entityType))
        {
            query = query.Where(e => e.EntityType == entityType.ToLowerInvariant());
        }

        // Filter by visibility if provided
        if (visibility.HasValue)
        {
            query = query.Where(e => e.Visibility == visibility.Value);
        }

        var entities = await query.ToListAsync(cancellationToken);

        // Filter based on visibility rules
        var visibleEntities = entities
            .Where(e => e.CanBeReadBy(userId.Value, true, membership.IsMaster))
            .Select(e => MapToDto(e))
            .ToList();

        return Ok(visibleEntities);
    }

    /// <summary>
    /// Get an entity by ID
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(LoreEntityDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<LoreEntityDto>> GetById(
        Guid campaignId,
        Guid id,
        CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId;
        if (userId == null)
            return Unauthorized();

        var membership = await GetCampaignMembershipAsync(campaignId, userId.Value, cancellationToken);
        if (membership == null)
            return NotFound("Campaign not found or you are not a member");

        var entity = await _dbContext.LoreEntities
            .FirstOrDefaultAsync(e => e.Id == id && e.CampaignId == campaignId && e.DeletedAt == null, cancellationToken);

        if (entity == null)
            return NotFound();

        if (!entity.CanBeReadBy(userId.Value, true, membership.IsMaster))
            return Forbid();

        return Ok(MapToDto(entity));
    }

    /// <summary>
    /// Create a new lore entity
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(LoreEntityDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<LoreEntityDto>> Create(
        Guid campaignId,
        [FromBody] CreateLoreEntityRequest request,
        CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId;
        if (userId == null)
            return Unauthorized();

        var membership = await GetCampaignMembershipAsync(campaignId, userId.Value, cancellationToken);
        if (membership == null)
            return NotFound("Campaign not found or you are not a member");

        // Determine ownership type based on role and request
        var ownershipType = request.OwnershipType ?? (membership.IsMaster ? OwnershipType.Master : OwnershipType.Player);
        
        // Players can only create player-owned entities
        if (!membership.IsMaster && ownershipType != OwnershipType.Player)
            return Forbid("Players can only create player-owned entities");

        // Parse visibility
        var visibility = request.Visibility ?? VisibilityLevel.Campaign;

        // Parse attributes and metadata from request
        JsonDocument? attributes = null;
        JsonDocument? metadata = null;

        if (request.Attributes != null)
        {
            attributes = JsonDocument.Parse(JsonSerializer.Serialize(request.Attributes));
        }

        if (request.Metadata != null)
        {
            metadata = JsonDocument.Parse(JsonSerializer.Serialize(request.Metadata));
        }

        var entity = LoreEntity.Create(
            campaignId: campaignId,
            ownerId: userId.Value,
            entityType: request.EntityType,
            name: request.Name,
            description: request.Description,
            ownershipType: ownershipType,
            visibility: visibility,
            isTemplate: request.IsTemplate ?? false,
            imageUrl: request.ImageUrl,
            attributes: attributes,
            metadata: metadata
        );

        _dbContext.LoreEntities.Add(entity);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("LoreEntity {EntityId} created by user {UserId} in campaign {CampaignId}", 
            entity.Id, userId, campaignId);

        return CreatedAtAction(
            nameof(GetById),
            new { campaignId, id = entity.Id },
            MapToDto(entity));
    }

    /// <summary>
    /// Update a lore entity
    /// </summary>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(LoreEntityDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<LoreEntityDto>> Update(
        Guid campaignId,
        Guid id,
        [FromBody] UpdateLoreEntityRequest request,
        CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId;
        if (userId == null)
            return Unauthorized();

        var membership = await GetCampaignMembershipAsync(campaignId, userId.Value, cancellationToken);
        if (membership == null)
            return NotFound("Campaign not found or you are not a member");

        var entity = await _dbContext.LoreEntities
            .FirstOrDefaultAsync(e => e.Id == id && e.CampaignId == campaignId && e.DeletedAt == null, cancellationToken);

        if (entity == null)
            return NotFound();

        if (!entity.CanBeWrittenBy(userId.Value, membership.IsMaster))
            return Forbid("You don't have permission to edit this entity");

        // Parse attributes and metadata
        JsonDocument? attributes = null;
        JsonDocument? metadata = null;

        if (request.Attributes != null)
        {
            attributes = JsonDocument.Parse(JsonSerializer.Serialize(request.Attributes));
        }

        if (request.Metadata != null)
        {
            metadata = JsonDocument.Parse(JsonSerializer.Serialize(request.Metadata));
        }

        entity.Update(
            name: request.Name,
            description: request.Description,
            visibility: request.Visibility,
            imageUrl: request.ImageUrl,
            attributes: attributes,
            metadata: metadata
        );

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("LoreEntity {EntityId} updated by user {UserId}", id, userId);

        return Ok(MapToDto(entity));
    }

    /// <summary>
    /// Delete a lore entity (soft delete)
    /// </summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Delete(
        Guid campaignId,
        Guid id,
        CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId;
        if (userId == null)
            return Unauthorized();

        var membership = await GetCampaignMembershipAsync(campaignId, userId.Value, cancellationToken);
        if (membership == null)
            return NotFound("Campaign not found or you are not a member");

        var entity = await _dbContext.LoreEntities
            .FirstOrDefaultAsync(e => e.Id == id && e.CampaignId == campaignId && e.DeletedAt == null, cancellationToken);

        if (entity == null)
            return NotFound();

        if (!entity.CanBeWrittenBy(userId.Value, membership.IsMaster))
            return Forbid("You don't have permission to delete this entity");

        entity.SoftDelete();
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("LoreEntity {EntityId} deleted by user {UserId}", id, userId);

        return NoContent();
    }

    /// <summary>
    /// Change entity visibility
    /// </summary>
    [HttpPatch("{id:guid}/visibility")]
    [ProducesResponseType(typeof(LoreEntityDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<LoreEntityDto>> ChangeVisibility(
        Guid campaignId,
        Guid id,
        [FromBody] ChangeVisibilityRequest request,
        CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId;
        if (userId == null)
            return Unauthorized();

        var membership = await GetCampaignMembershipAsync(campaignId, userId.Value, cancellationToken);
        if (membership == null)
            return NotFound("Campaign not found or you are not a member");

        var entity = await _dbContext.LoreEntities
            .FirstOrDefaultAsync(e => e.Id == id && e.CampaignId == campaignId && e.DeletedAt == null, cancellationToken);

        if (entity == null)
            return NotFound();

        if (!entity.CanBeWrittenBy(userId.Value, membership.IsMaster))
            return Forbid("You don't have permission to change visibility");

        entity.ChangeVisibility(request.Visibility);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Ok(MapToDto(entity));
    }

    private async Task<CampaignMember?> GetCampaignMembershipAsync(
        Guid campaignId, 
        Guid userId, 
        CancellationToken cancellationToken)
    {
        return await _dbContext.CampaignMembers
            .Include(cm => cm.Campaign)
            .FirstOrDefaultAsync(cm => 
                cm.CampaignId == campaignId && 
                cm.UserId == userId && 
                cm.Campaign.DeletedAt == null, 
                cancellationToken);
    }

    private static LoreEntityDto MapToDto(LoreEntity entity)
    {
        return new LoreEntityDto
        {
            Id = entity.Id,
            CampaignId = entity.CampaignId,
            OwnerId = entity.OwnerId,
            EntityType = entity.EntityType,
            Name = entity.Name,
            Description = entity.Description,
            OwnershipType = entity.OwnershipType,
            Visibility = entity.Visibility,
            IsTemplate = entity.IsTemplate,
            ImageUrl = entity.ImageUrl,
            Attributes = DeserializeJsonDocument(entity.Attributes),
            Metadata = DeserializeJsonDocument(entity.Metadata),
            CreatedAt = entity.CreatedAt,
            UpdatedAt = entity.UpdatedAt
        };
    }

    private static Dictionary<string, object>? DeserializeJsonDocument(JsonDocument? doc)
    {
        if (doc == null) return null;
        return JsonSerializer.Deserialize<Dictionary<string, object>>(doc.RootElement.GetRawText());
    }
}

// DTOs
public record LoreEntityDto
{
    public Guid Id { get; init; }
    public Guid CampaignId { get; init; }
    public Guid OwnerId { get; init; }
    public string EntityType { get; init; } = null!;
    public string Name { get; init; } = null!;
    public string? Description { get; init; }
    public OwnershipType OwnershipType { get; init; }
    public VisibilityLevel Visibility { get; init; }
    public bool IsTemplate { get; init; }
    public string? ImageUrl { get; init; }
    public Dictionary<string, object>? Attributes { get; init; }
    public Dictionary<string, object>? Metadata { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? UpdatedAt { get; init; }
}

public record CreateLoreEntityRequest
{
    public string EntityType { get; init; } = null!;
    public string Name { get; init; } = null!;
    public string? Description { get; init; }
    public OwnershipType? OwnershipType { get; init; }
    public VisibilityLevel? Visibility { get; init; }
    public bool? IsTemplate { get; init; }
    public string? ImageUrl { get; init; }
    public Dictionary<string, object>? Attributes { get; init; }
    public Dictionary<string, object>? Metadata { get; init; }
}

public record UpdateLoreEntityRequest
{
    public string Name { get; init; } = null!;
    public string? Description { get; init; }
    public VisibilityLevel? Visibility { get; init; }
    public string? ImageUrl { get; init; }
    public Dictionary<string, object>? Attributes { get; init; }
    public Dictionary<string, object>? Metadata { get; init; }
}

public record ChangeVisibilityRequest
{
    public VisibilityLevel Visibility { get; init; }
}
