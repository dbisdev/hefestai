namespace Loremaster.Domain.ValueObjects;

/// <summary>
/// Result of entity field generation via RAG.
/// Immutable value object containing generated field values.
/// </summary>
public sealed class EntityGenerationResult
{
    /// <summary>
    /// Whether the generation was successful.
    /// </summary>
    public bool Success { get; }
    
    /// <summary>
    /// Generated field values keyed by field name.
    /// </summary>
    public IReadOnlyDictionary<string, object?> GeneratedFields { get; }
    
    /// <summary>
    /// Generated entity name suggestion.
    /// </summary>
    public string? SuggestedName { get; }
    
    /// <summary>
    /// Generated entity description.
    /// </summary>
    public string? SuggestedDescription { get; }
    
    /// <summary>
    /// RAG context chunks used for generation.
    /// </summary>
    public IReadOnlyList<string> ContextChunks { get; }
    
    /// <summary>
    /// Error message if generation failed.
    /// </summary>
    public string? ErrorMessage { get; }
    
    /// <summary>
    /// Token usage statistics for tracking.
    /// </summary>
    public GenerationTokenUsage? TokenUsage { get; }

    private EntityGenerationResult(
        bool success,
        IReadOnlyDictionary<string, object?> generatedFields,
        string? suggestedName,
        string? suggestedDescription,
        IReadOnlyList<string> contextChunks,
        string? errorMessage,
        GenerationTokenUsage? tokenUsage)
    {
        Success = success;
        GeneratedFields = generatedFields;
        SuggestedName = suggestedName;
        SuggestedDescription = suggestedDescription;
        ContextChunks = contextChunks;
        ErrorMessage = errorMessage;
        TokenUsage = tokenUsage;
    }

    /// <summary>
    /// Creates a successful generation result.
    /// </summary>
    public static EntityGenerationResult Successful(
        IDictionary<string, object?> generatedFields,
        string? suggestedName = null,
        string? suggestedDescription = null,
        IEnumerable<string>? contextChunks = null,
        GenerationTokenUsage? tokenUsage = null)
    {
        return new EntityGenerationResult(
            success: true,
            generatedFields: generatedFields.AsReadOnly(),
            suggestedName: suggestedName?.Trim(),
            suggestedDescription: suggestedDescription?.Trim(),
            contextChunks: contextChunks?.ToList().AsReadOnly() ?? Array.Empty<string>().AsReadOnly(),
            errorMessage: null,
            tokenUsage: tokenUsage);
    }

    /// <summary>
    /// Creates a failed generation result.
    /// </summary>
    public static EntityGenerationResult Failed(string errorMessage)
    {
        if (string.IsNullOrWhiteSpace(errorMessage))
            throw new ArgumentException("Error message cannot be empty", nameof(errorMessage));

        return new EntityGenerationResult(
            success: false,
            generatedFields: new Dictionary<string, object?>().AsReadOnly(),
            suggestedName: null,
            suggestedDescription: null,
            contextChunks: Array.Empty<string>().AsReadOnly(),
            errorMessage: errorMessage.Trim(),
            tokenUsage: null);
    }

    /// <summary>
    /// Gets a generated field value by name.
    /// </summary>
    public T? GetFieldValue<T>(string fieldName)
    {
        if (!GeneratedFields.TryGetValue(fieldName, out var value))
            return default;
        
        if (value is T typedValue)
            return typedValue;
        
        // Try conversion
        try
        {
            return (T)Convert.ChangeType(value!, typeof(T));
        }
        catch
        {
            return default;
        }
    }

    /// <summary>
    /// Checks if a field was generated.
    /// </summary>
    public bool HasField(string fieldName)
    {
        return GeneratedFields.ContainsKey(fieldName);
    }
}

/// <summary>
/// Token usage statistics for generation requests.
/// </summary>
public sealed record GenerationTokenUsage(
    int PromptTokens,
    int CompletionTokens,
    int TotalTokens);

/// <summary>
/// Result of entity image generation.
/// </summary>
public sealed class EntityImageGenerationResult
{
    /// <summary>
    /// Whether the generation was successful.
    /// </summary>
    public bool Success { get; }
    
    /// <summary>
    /// Base64-encoded image data.
    /// </summary>
    public string? ImageBase64 { get; }
    
    /// <summary>
    /// Data URL for immediate display.
    /// </summary>
    public string? ImageDataUrl { get; }
    
    /// <summary>
    /// Permanent URL after storage (if stored).
    /// </summary>
    public string? StoredImageUrl { get; }
    
    /// <summary>
    /// The prompt used for generation.
    /// </summary>
    public string? GeneratedPrompt { get; }
    
    /// <summary>
    /// Error message if generation failed.
    /// </summary>
    public string? ErrorMessage { get; }

    private EntityImageGenerationResult(
        bool success,
        string? imageBase64,
        string? imageDataUrl,
        string? storedImageUrl,
        string? generatedPrompt,
        string? errorMessage)
    {
        Success = success;
        ImageBase64 = imageBase64;
        ImageDataUrl = imageDataUrl;
        StoredImageUrl = storedImageUrl;
        GeneratedPrompt = generatedPrompt;
        ErrorMessage = errorMessage;
    }

    /// <summary>
    /// Creates a successful image generation result.
    /// </summary>
    public static EntityImageGenerationResult Successful(
        string imageBase64,
        string? storedImageUrl = null,
        string? generatedPrompt = null)
    {
        if (string.IsNullOrWhiteSpace(imageBase64))
            throw new ArgumentException("Image data cannot be empty", nameof(imageBase64));

        var dataUrl = $"data:image/png;base64,{imageBase64}";
        
        return new EntityImageGenerationResult(
            success: true,
            imageBase64: imageBase64,
            imageDataUrl: dataUrl,
            storedImageUrl: storedImageUrl,
            generatedPrompt: generatedPrompt,
            errorMessage: null);
    }

    /// <summary>
    /// Creates a failed image generation result.
    /// </summary>
    public static EntityImageGenerationResult Failed(string errorMessage)
    {
        if (string.IsNullOrWhiteSpace(errorMessage))
            throw new ArgumentException("Error message cannot be empty", nameof(errorMessage));

        return new EntityImageGenerationResult(
            success: false,
            imageBase64: null,
            imageDataUrl: null,
            storedImageUrl: null,
            generatedPrompt: null,
            errorMessage: errorMessage.Trim());
    }

    /// <summary>
    /// Gets the best available URL (stored > data URL).
    /// </summary>
    public string? GetBestUrl() => StoredImageUrl ?? ImageDataUrl;
}
