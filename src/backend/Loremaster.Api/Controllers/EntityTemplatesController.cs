using Loremaster.Application.Features.EntityTemplates.Commands.ConfirmTemplate;
using Loremaster.Application.Features.EntityTemplates.Commands.CreateTemplate;
using Loremaster.Application.Features.EntityTemplates.Commands.DeleteTemplate;
using Loremaster.Application.Features.EntityTemplates.Commands.ExtractTemplatesFromManual;
using Loremaster.Application.Features.EntityTemplates.Commands.UpdateTemplate;
using Loremaster.Application.Features.EntityTemplates.DTOs;
using Loremaster.Application.Features.EntityTemplates.Queries.GetTemplateById;
using Loremaster.Application.Features.EntityTemplates.Queries.GetTemplatesByGameSystem;
using Loremaster.Application.Common.Interfaces;
using Loremaster.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Loremaster.Api.Controllers;

/// <summary>
/// Controller for managing entity templates.
/// Templates define the schema (fields) for creating entities of specific types.
/// Templates must be confirmed before they can be used for entity creation.
/// 
/// Authorization:
/// - GET endpoints: Require Player role (Players need templates to view entities)
/// - Write endpoints (POST/PUT/DELETE): Require Master role
/// </summary>
[ApiController]
[Route("api/game-systems/{gameSystemId:guid}/templates")]
[Authorize] // Base authorization - must be authenticated
public class EntityTemplatesController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly IGameSystemRepository _gameSystemRepository;

    public EntityTemplatesController(IMediator mediator, IGameSystemRepository gameSystemRepository)
    {
        _mediator = mediator;
        _gameSystemRepository = gameSystemRepository;
    }

    /// <summary>
    /// Get all templates for a game system.
    /// Accessible by Players to view templates when looking at entities.
    /// </summary>
    /// <param name="gameSystemId">The game system ID.</param>
    /// <param name="status">Optional status filter.</param>
    /// <param name="confirmedOnly">If true, only return confirmed templates.</param>
    /// <param name="includeAll">If true, ignore status and return all templates (Admins only).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    [HttpGet]
    [Authorize(Policy = "RequirePlayerRole")]
    [ProducesResponseType(typeof(GetTemplatesByGameSystemResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<GetTemplatesByGameSystemResult>> GetTemplates(
        [FromRoute] Guid gameSystemId,
        [FromQuery] TemplateStatus? status = null,
        [FromQuery] bool confirmedOnly = false,
        [FromQuery] bool includeAll = false,
        CancellationToken cancellationToken = default)
    {
        var userId = GetCurrentUserId();
        
        var query = new GetTemplatesByGameSystemQuery(
            gameSystemId, 
            userId, 
            status, 
            confirmedOnly,
            includeAll);
        
        var result = await _mediator.Send(query, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Get a specific template by ID.
    /// Accessible by Players to view template details.
    /// </summary>
    /// <param name="gameSystemId">The game system ID.</param>
    /// <param name="templateId">The template ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    [HttpGet("{templateId:guid}")]
    [Authorize(Policy = "RequirePlayerRole")]
    [ProducesResponseType(typeof(EntityTemplateDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<EntityTemplateDto>> GetTemplate(
        [FromRoute] Guid gameSystemId,
        [FromRoute] Guid templateId,
        CancellationToken cancellationToken = default)
    {
        var userId = GetCurrentUserId();
        var isAdmin = IsCurrentUserAdmin();
        
        var query = new GetTemplateByIdQuery(templateId, gameSystemId, userId, isAdmin);
        var result = await _mediator.Send(query, cancellationToken);
        
        if (result == null)
        {
            return NotFound($"Template with ID {templateId} not found");
        }
        
        // Verify game system matches
        if (result.GameSystemId != gameSystemId)
        {
            return NotFound($"Template with ID {templateId} not found in game system {gameSystemId}");
        }
        
        return Ok(result);
    }

    /// <summary>
    /// Extract entity templates from uploaded manuals using RAG.
    /// Analyzes the game system's manuals to detect entity types and their fields.
    /// </summary>
    /// <param name="gameSystemId">The game system ID.</param>
    /// <param name="request">Optional extraction parameters.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    [HttpPost("extract")]
    [Authorize(Policy = "RequireMasterRole")]
    [ProducesResponseType(typeof(ExtractTemplatesResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ExtractTemplatesResult>> ExtractTemplates(
        [FromRoute] Guid gameSystemId,
        [FromBody] ExtractTemplatesRequest? request = null,
        CancellationToken cancellationToken = default)
    {
        var userId = GetCurrentUserId();
        var isAdmin = IsCurrentUserAdmin();
        
        // Check if user owns the game system
        var gameSystem = await _gameSystemRepository.GetByIdAsync(gameSystemId, cancellationToken);
        var isSystemOwner = gameSystem?.OwnerId == userId;
        
        // Only Admin or system owner can extract templates
        if (!isAdmin && !isSystemOwner)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new { error = "Only the system owner or an Admin can extract templates" });
        }
        
        // Use the system owner's ID for the templates (not the current user's ID)
        // This ensures templates belong to the system, not the person who extracted them
        var templateOwnerId = gameSystem!.OwnerId;
        
        var command = new ExtractTemplatesFromManualCommand(
            gameSystemId, 
            templateOwnerId,  // Owner ID for templates (system owner)
            userId,          // Current user ID for document search
            request?.SourceDocumentId,
            isAdmin,
            isSystemOwner);
        
        try
        {
            var result = await _mediator.Send(command, cancellationToken);
            
            if (!string.IsNullOrEmpty(result.ErrorMessage))
            {
                return BadRequest(result);
            }
            
            return Ok(result);
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new { error = ex.Message });
        }
        catch (ArgumentException ex)
        {
            return NotFound(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Update a template's metadata and field definitions.
    /// Only allowed for templates in Draft or PendingReview status, unless user is Admin.
    /// </summary>
    /// <param name="gameSystemId">The game system ID.</param>
    /// <param name="templateId">The template ID to update.</param>
    /// <param name="request">The update request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    [HttpPut("{templateId:guid}")]
    [Authorize(Policy = "RequireMasterRole")]
    [ProducesResponseType(typeof(UpdateTemplateResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<UpdateTemplateResult>> UpdateTemplate(
        [FromRoute] Guid gameSystemId,
        [FromRoute] Guid templateId,
        [FromBody] UpdateTemplateRequest request,
        CancellationToken cancellationToken = default)
    {
        var userId = GetCurrentUserId();
        var isAdmin = IsCurrentUserAdmin();
        
        var command = new UpdateTemplateCommand(
            templateId,
            gameSystemId,
            userId,
            request.DisplayName,
            request.Description,
            request.IconHint,
            request.Version,
            request.Fields,
            isAdmin);
        
        try
        {
            var result = await _mediator.Send(command, cancellationToken);
            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            return NotFound(ex.Message);
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new { error = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Confirm a template, making it available for entity creation.
    /// </summary>
    /// <param name="gameSystemId">The game system ID.</param>
    /// <param name="templateId">The template ID to confirm.</param>
    /// <param name="request">Optional confirmation notes.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    [HttpPost("{templateId:guid}/confirm")]
    [Authorize(Policy = "RequireMasterRole")]
    [ProducesResponseType(typeof(ConfirmTemplateResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ConfirmTemplateResult>> ConfirmTemplate(
        [FromRoute] Guid gameSystemId,
        [FromRoute] Guid templateId,
        [FromBody] ConfirmTemplateRequest? request = null,
        CancellationToken cancellationToken = default)
    {
        var userId = GetCurrentUserId();
        
        var command = new ConfirmTemplateCommand(
            templateId,
            gameSystemId,
            userId, 
            request?.Notes);
        
        try
        {
            var result = await _mediator.Send(command, cancellationToken);
            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            return NotFound(ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    /// <summary>
    /// Create a new entity template manually.
    /// </summary>
    /// <param name="gameSystemId">The game system ID.</param>
    /// <param name="request">The template creation request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    [HttpPost]
    [Authorize(Policy = "RequireMasterRole")]
    [ProducesResponseType(typeof(CreateTemplateResult), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<CreateTemplateResult>> CreateTemplate(
        [FromRoute] Guid gameSystemId,
        [FromBody] CreateTemplateRequest request,
        CancellationToken cancellationToken = default)
    {
        var userId = GetCurrentUserId();
        
        var command = new CreateTemplateCommand(
            gameSystemId,
            userId,
            request.EntityTypeName,
            request.DisplayName,
            request.Description,
            request.IconHint,
            request.Version,
            request.Fields);
        
        try
        {
            var result = await _mediator.Send(command, cancellationToken);
            return CreatedAtAction(
                nameof(GetTemplate),
                new { gameSystemId, templateId = result.TemplateId },
                result);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Forbid(ex.Message);
        }
    }

    /// <summary>
    /// Delete an entity template.
    /// </summary>
    /// <param name="gameSystemId">The game system ID.</param>
    /// <param name="templateId">The template ID to delete.</param>
    /// <param name="force">If true, delete even if entities are using this template.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    [HttpDelete("{templateId:guid}")]
    [Authorize(Policy = "RequireMasterRole")]
    [ProducesResponseType(typeof(DeleteTemplateResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<DeleteTemplateResult>> DeleteTemplate(
        [FromRoute] Guid gameSystemId,
        [FromRoute] Guid templateId,
        [FromQuery] bool force = false,
        CancellationToken cancellationToken = default)
    {
        var userId = GetCurrentUserId();
        
        var command = new DeleteTemplateCommand(templateId, gameSystemId, userId, force);
        
        try
        {
            var result = await _mediator.Send(command, cancellationToken);
            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            return NotFound(ex.Message);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Forbid(ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    private Guid GetCurrentUserId()
    {
        // Try JWT standard claim "sub" first (when MapInboundClaims = false)
        // Fall back to ClaimTypes.NameIdentifier for compatibility
        var userIdClaim = User.FindFirst("sub")?.Value
            ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        
        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
        {
            throw new UnauthorizedAccessException("Invalid user token");
        }

        return userId;
    }

    /// <summary>
    /// Checks if the current user has the Admin role.
    /// </summary>
    private bool IsCurrentUserAdmin()
    {
        var roleClaim = User.FindFirst("role")?.Value
            ?? User.FindFirst(ClaimTypes.Role)?.Value;
        
        return roleClaim == "Admin";
    }
}

#region Request DTOs

/// <summary>
/// Request for template extraction.
/// </summary>
public record ExtractTemplatesRequest
{
    /// <summary>
    /// Optional specific document to analyze. If null, analyzes all manuals.
    /// </summary>
    public Guid? SourceDocumentId { get; init; }
}

/// <summary>
/// Request to update a template.
/// </summary>
public record UpdateTemplateRequest
{
    public string DisplayName { get; init; } = null!;
    public string? Description { get; init; }
    public string? IconHint { get; init; }
    public string? Version { get; init; }
    public IReadOnlyList<FieldDefinitionDto>? Fields { get; init; }
}

/// <summary>
/// Request to confirm a template.
/// </summary>
public record ConfirmTemplateRequest
{
    public string? Notes { get; init; }
}

/// <summary>
/// Request to create a new template manually.
/// </summary>
public record CreateTemplateRequest
{
    /// <summary>
    /// The entity type name (e.g., "character", "vehicle").
    /// Will be normalized to lowercase with underscores.
    /// </summary>
    public string EntityTypeName { get; init; } = null!;
    
    /// <summary>
    /// Human-readable display name.
    /// </summary>
    public string DisplayName { get; init; } = null!;
    
    /// <summary>
    /// Optional description of this entity type.
    /// </summary>
    public string? Description { get; init; }
    
    /// <summary>
    /// Optional icon or category hint for UI display.
    /// </summary>
    public string? IconHint { get; init; }
    
    /// <summary>
    /// Optional version identifier.
    /// </summary>
    public string? Version { get; init; }
    
    /// <summary>
    /// Optional initial field definitions.
    /// </summary>
    public IReadOnlyList<FieldDefinitionDto>? Fields { get; init; }
}

#endregion
