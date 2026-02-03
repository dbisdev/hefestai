using Loremaster.Application.Features.EntityGeneration.DTOs;
using MediatR;

namespace Loremaster.Application.Features.EntityGeneration.Commands.GenerateEntityImage;

/// <summary>
/// Command to generate an image for an existing entity using RAG and AI.
/// Supports both initial generation and regeneration.
/// </summary>
public record GenerateEntityImageCommand : IRequest<GenerateEntityImageResult>
{
    /// <summary>
    /// The campaign ID for context and authorization.
    /// </summary>
    public Guid CampaignId { get; init; }
    
    /// <summary>
    /// The entity ID to generate an image for.
    /// </summary>
    public Guid EntityId { get; init; }
    
    /// <summary>
    /// Style hint for image generation (fantasy, realistic, anime, sketch).
    /// Defaults to "fantasy" if not specified.
    /// </summary>
    public string? Style { get; init; }
    
    /// <summary>
    /// Whether this is a regeneration request (replaces existing image).
    /// </summary>
    public bool IsRegeneration { get; init; }
}
