namespace Loremaster.Domain.ValueObjects;

/// <summary>
/// Configuration for entity field generation via RAG.
/// Immutable value object that defines how an entity should be generated.
/// </summary>
public sealed class EntityGenerationConfig : IEquatable<EntityGenerationConfig>
{
    /// <summary>
    /// The game system ID to scope RAG queries.
    /// </summary>
    public Guid GameSystemId { get; }
    
    /// <summary>
    /// The entity template ID defining the field structure.
    /// </summary>
    public Guid TemplateId { get; }
    
    /// <summary>
    /// The entity type name (normalized).
    /// </summary>
    public string EntityTypeName { get; }
    
    /// <summary>
    /// Optional user prompt to guide generation (e.g., "a brave elven warrior").
    /// </summary>
    public string? UserPrompt { get; }
    
    /// <summary>
    /// Specific field names to generate. If empty, generates all fields.
    /// </summary>
    public IReadOnlyList<string> FieldsToGenerate { get; }
    
    /// <summary>
    /// Existing field values to preserve (not regenerate).
    /// </summary>
    public IReadOnlyDictionary<string, object?> ExistingValues { get; }
    
    /// <summary>
    /// Temperature for AI generation (0.0 = deterministic, 1.0 = creative).
    /// Default is 0.7 for balanced creativity.
    /// </summary>
    public float Temperature { get; }
    
    /// <summary>
    /// Whether to include image generation.
    /// </summary>
    public bool IncludeImageGeneration { get; }
    
    /// <summary>
    /// Style hint for image generation (e.g., "fantasy", "realistic", "anime").
    /// </summary>
    public string? ImageStyle { get; }

    private EntityGenerationConfig(
        Guid gameSystemId,
        Guid templateId,
        string entityTypeName,
        string? userPrompt,
        IReadOnlyList<string> fieldsToGenerate,
        IReadOnlyDictionary<string, object?> existingValues,
        float temperature,
        bool includeImageGeneration,
        string? imageStyle)
    {
        GameSystemId = gameSystemId;
        TemplateId = templateId;
        EntityTypeName = entityTypeName;
        UserPrompt = userPrompt;
        FieldsToGenerate = fieldsToGenerate;
        ExistingValues = existingValues;
        Temperature = temperature;
        IncludeImageGeneration = includeImageGeneration;
        ImageStyle = imageStyle;
    }

    /// <summary>
    /// Creates a new entity generation configuration.
    /// </summary>
    public static EntityGenerationConfig Create(
        Guid gameSystemId,
        Guid templateId,
        string entityTypeName,
        string? userPrompt = null,
        IEnumerable<string>? fieldsToGenerate = null,
        IDictionary<string, object?>? existingValues = null,
        float temperature = 0.7f,
        bool includeImageGeneration = false,
        string? imageStyle = null)
    {
        if (gameSystemId == Guid.Empty)
            throw new ArgumentException("Game system ID cannot be empty", nameof(gameSystemId));
        
        if (templateId == Guid.Empty)
            throw new ArgumentException("Template ID cannot be empty", nameof(templateId));
        
        if (string.IsNullOrWhiteSpace(entityTypeName))
            throw new ArgumentException("Entity type name cannot be empty", nameof(entityTypeName));
        
        if (temperature < 0.0f || temperature > 1.0f)
            throw new ArgumentOutOfRangeException(nameof(temperature), "Temperature must be between 0.0 and 1.0");

        return new EntityGenerationConfig(
            gameSystemId,
            templateId,
            entityTypeName.ToLowerInvariant().Trim(),
            userPrompt?.Trim(),
            fieldsToGenerate?.ToList().AsReadOnly() ?? Array.Empty<string>().AsReadOnly(),
            existingValues != null 
                ? new Dictionary<string, object?>(existingValues).AsReadOnly() 
                : new Dictionary<string, object?>().AsReadOnly(),
            temperature,
            includeImageGeneration,
            imageStyle?.Trim());
    }

    /// <summary>
    /// Returns true if specific fields are requested, false if all fields should be generated.
    /// </summary>
    public bool HasSpecificFields => FieldsToGenerate.Count > 0;

    /// <summary>
    /// Checks if a specific field should be generated.
    /// </summary>
    public bool ShouldGenerateField(string fieldName)
    {
        // If no specific fields requested, generate all
        if (!HasSpecificFields)
            return true;
        
        return FieldsToGenerate.Contains(fieldName, StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Gets an existing value for a field, if any.
    /// </summary>
    public object? GetExistingValue(string fieldName)
    {
        return ExistingValues.TryGetValue(fieldName, out var value) ? value : null;
    }

    public bool Equals(EntityGenerationConfig? other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;
        return GameSystemId == other.GameSystemId &&
               TemplateId == other.TemplateId &&
               EntityTypeName == other.EntityTypeName;
    }

    public override bool Equals(object? obj) => Equals(obj as EntityGenerationConfig);

    public override int GetHashCode() => HashCode.Combine(GameSystemId, TemplateId, EntityTypeName);
}
