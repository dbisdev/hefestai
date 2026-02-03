namespace Loremaster.Application.Features.EntityGeneration.DTOs;

/// <summary>
/// Result of entity field generation.
/// </summary>
public record GenerateEntityFieldsResult
{
    /// <summary>
    /// Whether the generation was successful.
    /// </summary>
    public bool Success { get; init; }
    
    /// <summary>
    /// Generated field values keyed by field name.
    /// </summary>
    public IDictionary<string, object?> GeneratedFields { get; init; } = new Dictionary<string, object?>();
    
    /// <summary>
    /// Suggested entity name.
    /// </summary>
    public string? SuggestedName { get; init; }
    
    /// <summary>
    /// Suggested entity description.
    /// </summary>
    public string? SuggestedDescription { get; init; }
    
    /// <summary>
    /// Generated image data URL (if image generation was requested).
    /// </summary>
    public string? ImageDataUrl { get; init; }
    
    /// <summary>
    /// Permanent image URL (if image was stored).
    /// </summary>
    public string? ImageUrl { get; init; }
    
    /// <summary>
    /// RAG context sources used for generation (for transparency).
    /// </summary>
    public IReadOnlyList<RagSourceInfo> ContextSources { get; init; } = Array.Empty<RagSourceInfo>();
    
    /// <summary>
    /// Error message if generation failed.
    /// </summary>
    public string? ErrorMessage { get; init; }
    
    /// <summary>
    /// The template ID used for generation.
    /// </summary>
    public Guid TemplateId { get; init; }
    
    /// <summary>
    /// The entity type name.
    /// </summary>
    public string EntityTypeName { get; init; } = null!;

    /// <summary>
    /// Creates a successful result.
    /// </summary>
    public static GenerateEntityFieldsResult Successful(
        Guid templateId,
        string entityTypeName,
        IDictionary<string, object?> generatedFields,
        string? suggestedName = null,
        string? suggestedDescription = null,
        string? imageDataUrl = null,
        string? imageUrl = null,
        IEnumerable<RagSourceInfo>? contextSources = null)
    {
        return new GenerateEntityFieldsResult
        {
            Success = true,
            TemplateId = templateId,
            EntityTypeName = entityTypeName,
            GeneratedFields = generatedFields,
            SuggestedName = suggestedName,
            SuggestedDescription = suggestedDescription,
            ImageDataUrl = imageDataUrl,
            ImageUrl = imageUrl,
            ContextSources = contextSources?.ToList().AsReadOnly() ?? (IReadOnlyList<RagSourceInfo>)Array.Empty<RagSourceInfo>()
        };
    }

    /// <summary>
    /// Creates a failed result.
    /// </summary>
    public static GenerateEntityFieldsResult Failed(string errorMessage, Guid templateId = default, string? entityTypeName = null)
    {
        return new GenerateEntityFieldsResult
        {
            Success = false,
            ErrorMessage = errorMessage,
            TemplateId = templateId,
            EntityTypeName = entityTypeName ?? string.Empty
        };
    }
}

/// <summary>
/// Information about a RAG source used in generation.
/// </summary>
public record RagSourceInfo(
    string Title,
    Guid DocumentId,
    float SimilarityScore);

/// <summary>
/// Result of entity image generation.
/// </summary>
public record GenerateEntityImageResult
{
    /// <summary>
    /// Whether the generation was successful.
    /// </summary>
    public bool Success { get; init; }
    
    /// <summary>
    /// Base64-encoded image data.
    /// </summary>
    public string? ImageBase64 { get; init; }
    
    /// <summary>
    /// Data URL for immediate display.
    /// </summary>
    public string? ImageDataUrl { get; init; }
    
    /// <summary>
    /// Permanent URL after storage.
    /// </summary>
    public string? StoredImageUrl { get; init; }
    
    /// <summary>
    /// The prompt used for generation.
    /// </summary>
    public string? GeneratedPrompt { get; init; }
    
    /// <summary>
    /// Error message if generation failed.
    /// </summary>
    public string? ErrorMessage { get; init; }
    
    /// <summary>
    /// The entity ID this image was generated for.
    /// </summary>
    public Guid EntityId { get; init; }

    /// <summary>
    /// Creates a successful result.
    /// </summary>
    public static GenerateEntityImageResult Successful(
        Guid entityId,
        string imageBase64,
        string? storedImageUrl = null,
        string? generatedPrompt = null)
    {
        return new GenerateEntityImageResult
        {
            Success = true,
            EntityId = entityId,
            ImageBase64 = imageBase64,
            ImageDataUrl = $"data:image/png;base64,{imageBase64}",
            StoredImageUrl = storedImageUrl,
            GeneratedPrompt = generatedPrompt
        };
    }

    /// <summary>
    /// Creates a failed result.
    /// </summary>
    public static GenerateEntityImageResult Failed(string errorMessage, Guid entityId = default)
    {
        return new GenerateEntityImageResult
        {
            Success = false,
            ErrorMessage = errorMessage,
            EntityId = entityId
        };
    }
}
