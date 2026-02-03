using Loremaster.Application.Features.EntityGeneration.DTOs;
using MediatR;

namespace Loremaster.Application.Features.EntityGeneration.Commands.GenerateEntityFields;

/// <summary>
/// Command to generate entity field values using RAG and confirmed templates.
/// This is the core EPIC 4.5 command for assisted entity creation.
/// </summary>
public record GenerateEntityFieldsCommand : IRequest<GenerateEntityFieldsResult>
{
    /// <summary>
    /// The campaign ID for context and authorization.
    /// </summary>
    public Guid CampaignId { get; init; }
    
    /// <summary>
    /// The entity type name (will be normalized to match template).
    /// </summary>
    public string EntityTypeName { get; init; } = null!;
    
    /// <summary>
    /// Optional user prompt to guide generation (e.g., "a wise old wizard").
    /// </summary>
    public string? UserPrompt { get; init; }
    
    /// <summary>
    /// Specific field names to generate. If empty/null, generates all fields.
    /// </summary>
    public IReadOnlyList<string>? FieldsToGenerate { get; init; }
    
    /// <summary>
    /// Existing field values to preserve (not regenerate).
    /// </summary>
    public IDictionary<string, object?>? ExistingValues { get; init; }
    
    /// <summary>
    /// Temperature for AI generation (0.0 = deterministic, 1.0 = creative).
    /// </summary>
    public float Temperature { get; init; } = 0.7f;
    
    /// <summary>
    /// Whether to include image generation with the fields.
    /// </summary>
    public bool IncludeImageGeneration { get; init; }
    
    /// <summary>
    /// Style hint for image generation (fantasy, realistic, anime).
    /// </summary>
    public string? ImageStyle { get; init; }
}
