using Loremaster.Application.Features.LoreEntities.Commands.ChangeEntityVisibility;
using Loremaster.Application.Features.LoreEntities.Commands.CreateLoreEntity;
using Loremaster.Application.Features.LoreEntities.Commands.DeleteLoreEntity;
using Loremaster.Application.Features.LoreEntities.Commands.TransferEntityOwnership;
using Loremaster.Application.Features.LoreEntities.Commands.UpdateLoreEntity;
using Loremaster.Application.Features.LoreEntities.DTOs;
using Loremaster.Application.Features.LoreEntities.Queries.GetCampaignEntities;
using Loremaster.Application.Features.LoreEntities.Queries.GetLoreEntityById;
using Loremaster.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Loremaster.Api.Controllers;

/// <summary>
/// Controller for managing lore entities (characters, NPCs, locations, items, etc.) within campaigns.
/// </summary>
[ApiController]
[Route("api/campaigns/{campaignId:guid}/[controller]")]
[Authorize]
public class EntitiesController : ControllerBase
{
    private readonly IMediator _mediator;

    public EntitiesController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Get visible entities in a campaign for the current user with optional pagination.
    /// </summary>
    /// <param name="campaignId">The campaign ID.</param>
    /// <param name="entityType">Optional filter by entity type.</param>
    /// <param name="visibility">Optional filter by visibility level.</param>
    /// <param name="search">Optional search term to filter by name or description.</param>
    /// <param name="page">Page number (1-based). If not specified, returns all results.</param>
    /// <param name="pageSize">Number of items per page. Default is 20, max is 100.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    [HttpGet]
    [ProducesResponseType(typeof(GetCampaignEntitiesResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<GetCampaignEntitiesResult>> GetCampaignEntities(
        Guid campaignId,
        [FromQuery] string? entityType,
        [FromQuery] VisibilityLevel? visibility,
        [FromQuery] string? search,
        [FromQuery] int? page,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var query = new GetCampaignEntitiesQuery(
            campaignId, 
            entityType, 
            visibility, 
            search,
            page, 
            pageSize);
        var result = await _mediator.Send(query, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Get an entity by ID.
    /// </summary>
    /// <param name="campaignId">The campaign ID.</param>
    /// <param name="id">The entity ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(LoreEntityDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<LoreEntityDto>> GetById(
        Guid campaignId,
        Guid id,
        CancellationToken cancellationToken)
    {
        var query = new GetLoreEntityByIdQuery(campaignId, id);
        var result = await _mediator.Send(query, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Create a new lore entity.
    /// </summary>
    /// <param name="campaignId">The campaign ID.</param>
    /// <param name="request">The creation request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    [HttpPost]
    [ProducesResponseType(typeof(LoreEntityDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<LoreEntityDto>> Create(
        Guid campaignId,
        [FromBody] CreateLoreEntityRequest request,
        CancellationToken cancellationToken)
    {
        var command = new CreateLoreEntityCommand(
            CampaignId: campaignId,
            EntityType: request.EntityType,
            Name: request.Name,
            Description: request.Description,
            OwnershipType: request.OwnershipType,
            Visibility: request.Visibility,
            IsTemplate: request.IsTemplate,
            ImageUrl: request.ImageUrl,
            Attributes: request.Attributes,
            Metadata: request.Metadata
        );
        
        var result = await _mediator.Send(command, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { campaignId, id = result.Id }, result);
    }

    /// <summary>
    /// Update a lore entity.
    /// </summary>
    /// <param name="campaignId">The campaign ID.</param>
    /// <param name="id">The entity ID.</param>
    /// <param name="request">The update request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
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
        var command = new UpdateLoreEntityCommand(
            CampaignId: campaignId,
            EntityId: id,
            Name: request.Name,
            Description: request.Description,
            Visibility: request.Visibility,
            ImageUrl: request.ImageUrl,
            Attributes: request.Attributes,
            Metadata: request.Metadata
        );
        
        var result = await _mediator.Send(command, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Delete a lore entity (soft delete).
    /// </summary>
    /// <param name="campaignId">The campaign ID.</param>
    /// <param name="id">The entity ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Delete(
        Guid campaignId,
        Guid id,
        CancellationToken cancellationToken)
    {
        await _mediator.Send(new DeleteLoreEntityCommand(campaignId, id), cancellationToken);
        return NoContent();
    }

    /// <summary>
    /// Change entity visibility.
    /// </summary>
    /// <param name="campaignId">The campaign ID.</param>
    /// <param name="id">The entity ID.</param>
    /// <param name="request">The visibility change request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
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
        var command = new ChangeEntityVisibilityCommand(campaignId, id, request.Visibility);
        var result = await _mediator.Send(command, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Transfer entity ownership to another campaign member.
    /// Only the campaign master can transfer entity ownership.
    /// </summary>
    /// <param name="campaignId">The campaign ID.</param>
    /// <param name="id">The entity ID.</param>
    /// <param name="request">The transfer ownership request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    [HttpPatch("{id:guid}/owner")]
    [ProducesResponseType(typeof(LoreEntityDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<LoreEntityDto>> TransferOwnership(
        Guid campaignId,
        Guid id,
        [FromBody] TransferOwnershipRequest request,
        CancellationToken cancellationToken)
    {
        var command = new TransferEntityOwnershipCommand(
            campaignId, 
            id, 
            request.NewOwnerId, 
            request.NewOwnershipType);
        var result = await _mediator.Send(command, cancellationToken);
        return Ok(result);
    }
}

#region Request DTOs

/// <summary>
/// Request to create a new lore entity.
/// </summary>
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

/// <summary>
/// Request to update a lore entity.
/// </summary>
public record UpdateLoreEntityRequest
{
    public string Name { get; init; } = null!;
    public string? Description { get; init; }
    public VisibilityLevel? Visibility { get; init; }
    public string? ImageUrl { get; init; }
    public Dictionary<string, object>? Attributes { get; init; }
    public Dictionary<string, object>? Metadata { get; init; }
}

/// <summary>
/// Request to change entity visibility.
/// </summary>
public record ChangeVisibilityRequest
{
    public VisibilityLevel Visibility { get; init; }
}

/// <summary>
/// Request to transfer entity ownership to another campaign member.
/// </summary>
public record TransferOwnershipRequest
{
    /// <summary>
    /// The ID of the new owner (must be a campaign member).
    /// </summary>
    public Guid NewOwnerId { get; init; }
    
    /// <summary>
    /// Optional new ownership type. If not specified, defaults based on new owner's role.
    /// </summary>
    public OwnershipType? NewOwnershipType { get; init; }
}

#endregion
