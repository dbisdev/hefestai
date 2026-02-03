using Loremaster.Application.Features.EntityGeneration.Commands.GenerateEntityFields;
using Loremaster.Application.Features.EntityGeneration.Commands.GenerateEntityImage;
using Loremaster.Application.Features.EntityGeneration.DTOs;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Loremaster.Api.Controllers;

/// <summary>
/// Controller for AI-assisted entity generation using RAG.
/// Part of EPIC 4.5 - Entity Assisted Generation.
/// </summary>
[ApiController]
[Route("api/campaigns/{campaignId:guid}/generation")]
[Authorize]
public class EntityGenerationController : ControllerBase
{
    private readonly IMediator _mediator;

    public EntityGenerationController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Generate entity field values using RAG and confirmed templates.
    /// Returns generated field values that can be used to create a new entity.
    /// </summary>
    /// <param name="campaignId">The campaign ID for context and authorization.</param>
    /// <param name="request">The generation request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Generated field values based on game system rules and templates.</returns>
    /// <response code="200">Generation completed (check Success flag for result).</response>
    /// <response code="400">Invalid request parameters.</response>
    /// <response code="403">User not authorized for this campaign.</response>
    /// <response code="404">Campaign not found.</response>
    [HttpPost("fields")]
    [ProducesResponseType(typeof(GenerateEntityFieldsResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<GenerateEntityFieldsResult>> GenerateEntityFields(
        Guid campaignId,
        [FromBody] GenerateEntityFieldsRequest request,
        CancellationToken cancellationToken)
    {
        var command = new GenerateEntityFieldsCommand
        {
            CampaignId = campaignId,
            EntityTypeName = request.EntityTypeName,
            UserPrompt = request.UserPrompt,
            FieldsToGenerate = request.FieldsToGenerate,
            ExistingValues = request.ExistingValues,
            Temperature = request.Temperature ?? 0.7f,
            IncludeImageGeneration = request.IncludeImageGeneration ?? false,
            ImageStyle = request.ImageStyle
        };

        var result = await _mediator.Send(command, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Generate an image for an existing entity using AI.
    /// </summary>
    /// <param name="campaignId">The campaign ID for context and authorization.</param>
    /// <param name="entityId">The entity ID to generate an image for.</param>
    /// <param name="request">The image generation request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Generated image data and URLs.</returns>
    /// <response code="200">Image generation completed (check Success flag for result).</response>
    /// <response code="400">Invalid request parameters.</response>
    /// <response code="403">User not authorized to modify this entity.</response>
    /// <response code="404">Campaign or entity not found.</response>
    [HttpPost("entities/{entityId:guid}/image")]
    [ProducesResponseType(typeof(GenerateEntityImageResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<GenerateEntityImageResult>> GenerateEntityImage(
        Guid campaignId,
        Guid entityId,
        [FromBody] GenerateEntityImageRequest request,
        CancellationToken cancellationToken)
    {
        var command = new GenerateEntityImageCommand
        {
            CampaignId = campaignId,
            EntityId = entityId,
            Style = request.Style,
            IsRegeneration = request.IsRegeneration ?? false
        };

        var result = await _mediator.Send(command, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Regenerate an image for an existing entity using AI.
    /// This is a convenience endpoint that sets IsRegeneration = true.
    /// </summary>
    /// <param name="campaignId">The campaign ID for context and authorization.</param>
    /// <param name="entityId">The entity ID to regenerate an image for.</param>
    /// <param name="request">The image regeneration request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Regenerated image data and URLs.</returns>
    [HttpPost("entities/{entityId:guid}/image/regenerate")]
    [ProducesResponseType(typeof(GenerateEntityImageResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<GenerateEntityImageResult>> RegenerateEntityImage(
        Guid campaignId,
        Guid entityId,
        [FromBody] RegenerateEntityImageRequest? request,
        CancellationToken cancellationToken)
    {
        var command = new GenerateEntityImageCommand
        {
            CampaignId = campaignId,
            EntityId = entityId,
            Style = request?.Style,
            IsRegeneration = true
        };

        var result = await _mediator.Send(command, cancellationToken);
        return Ok(result);
    }
}

#region Request DTOs

/// <summary>
/// Request to generate entity field values.
/// </summary>
public record GenerateEntityFieldsRequest
{
    /// <summary>
    /// The entity type name (e.g., "character", "vehicle", "location").
    /// </summary>
    public string EntityTypeName { get; init; } = null!;
    
    /// <summary>
    /// Optional user prompt to guide generation (e.g., "a wise old wizard").
    /// </summary>
    public string? UserPrompt { get; init; }
    
    /// <summary>
    /// Specific field names to generate. If empty/null, generates all fields.
    /// </summary>
    public List<string>? FieldsToGenerate { get; init; }
    
    /// <summary>
    /// Existing field values to preserve (not regenerate).
    /// </summary>
    public Dictionary<string, object?>? ExistingValues { get; init; }
    
    /// <summary>
    /// Temperature for AI generation (0.0 = deterministic, 1.0 = creative).
    /// Defaults to 0.7.
    /// </summary>
    public float? Temperature { get; init; }
    
    /// <summary>
    /// Whether to include image generation with the fields.
    /// </summary>
    public bool? IncludeImageGeneration { get; init; }
    
    /// <summary>
    /// Style hint for image generation (fantasy, realistic, anime, sketch).
    /// </summary>
    public string? ImageStyle { get; init; }
}

/// <summary>
/// Request to generate an entity image.
/// </summary>
public record GenerateEntityImageRequest
{
    /// <summary>
    /// Style hint for image generation (fantasy, realistic, anime, sketch).
    /// Defaults to "fantasy" if not specified.
    /// </summary>
    public string? Style { get; init; }
    
    /// <summary>
    /// Whether this is a regeneration request (replaces existing image).
    /// </summary>
    public bool? IsRegeneration { get; init; }
}

/// <summary>
/// Request to regenerate an entity image.
/// </summary>
public record RegenerateEntityImageRequest
{
    /// <summary>
    /// Style hint for image regeneration (fantasy, realistic, anime, sketch).
    /// </summary>
    public string? Style { get; init; }
}

#endregion
