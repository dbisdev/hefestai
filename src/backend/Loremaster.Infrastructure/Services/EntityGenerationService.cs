using System.Text.Json;
using Loremaster.Application.Common.Interfaces;
using Loremaster.Domain.Entities;
using Loremaster.Domain.Enums;
using Loremaster.Domain.ValueObjects;
using Microsoft.Extensions.Logging;

namespace Loremaster.Infrastructure.Services;

/// <summary>
/// Service for RAG-assisted entity generation.
/// Generates entity field values based on templates and game system manuals.
/// </summary>
public class EntityGenerationService : IEntityGenerationService
{
    private readonly IRagContextProvider _ragContextProvider;
    private readonly IEmbeddingService _embeddingService;
    private readonly IAiService _aiService;
    private readonly ILogger<EntityGenerationService> _logger;

    /// <summary>
    /// Maximum tokens for field generation response.
    /// </summary>
    private const int MaxFieldGenerationTokens = 4096;

    /// <summary>
    /// Maximum tokens for image prompt generation.
    /// </summary>
    private const int MaxImagePromptTokens = 512;

    public EntityGenerationService(
        IRagContextProvider ragContextProvider,
        IEmbeddingService embeddingService,
        IAiService aiService,
        ILogger<EntityGenerationService> logger)
    {
        _ragContextProvider = ragContextProvider ?? throw new ArgumentNullException(nameof(ragContextProvider));
        _embeddingService = embeddingService ?? throw new ArgumentNullException(nameof(embeddingService));
        _aiService = aiService ?? throw new ArgumentNullException(nameof(aiService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async Task<EntityGenerationResult> GenerateEntityFieldsAsync(
        EntityGenerationConfig config,
        EntityTemplate template,
        Guid ownerId,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Generating entity fields for template '{TemplateName}' ({TemplateId}), GameSystem: {GameSystemId}",
            template.DisplayName, template.Id, config.GameSystemId);

        try
        {
            // Validate template is confirmed
            if (!template.CanBeUsedForEntityCreation)
            {
                return EntityGenerationResult.Failed(
                    $"Template '{template.DisplayName}' is not confirmed and cannot be used for generation");
            }

            // Get RAG context from game system manuals
            var ragChunks = await _ragContextProvider.GetContextForEntityGenerationAsync(
                config.GameSystemId,
                ownerId,
                config.EntityTypeName,
                config.UserPrompt,
                maxChunks: 7,
                cancellationToken);

            // Build the generation prompt
            var fieldDefinitions = template.GetFieldDefinitions();
            var systemPrompt = BuildFieldGenerationSystemPrompt(template, fieldDefinitions);
            var userPrompt = BuildFieldGenerationUserPrompt(config, fieldDefinitions, ragChunks);

            // Generate using RAG-enhanced context if available
            string jsonResponse;
            TokenUsage? tokenUsage = null;

            if (ragChunks.Any())
            {
                // Use RAG generation with context
                var contextStrings = ragChunks.Select(c => c.Content).ToList();
                var ragResult = await _embeddingService.GenerateWithContextAsync(
                    userPrompt,
                    contextStrings,
                    systemPrompt,
                    config.Temperature,
                    MaxFieldGenerationTokens,
                    cancellationToken);

                jsonResponse = ragResult.Answer;
                tokenUsage = ragResult.Usage;
            }
            else
            {
                // Fall back to direct JSON generation without RAG context
                _logger.LogWarning(
                    "No RAG context available for entity type '{EntityType}', generating without manual context",
                    config.EntityTypeName);

                var jsonResult = await _aiService.GenerateJsonAsync(
                    userPrompt,
                    systemPrompt,
                    config.Temperature,
                    MaxFieldGenerationTokens,
                    cancellationToken);

                jsonResponse = jsonResult.Json;
                tokenUsage = jsonResult.Usage;
            }

            // Parse the generated JSON
            var generationResult = ParseGeneratedFields(jsonResponse, fieldDefinitions, config);

            if (!generationResult.Success)
            {
                return generationResult;
            }

            // Add token usage and context info
            var contextChunkStrings = ragChunks.Select(c => $"[{c.SourceTitle}]: {c.Content}").ToList();
            
            return EntityGenerationResult.Successful(
                new Dictionary<string, object?>(generationResult.GeneratedFields),
                generationResult.SuggestedName,
                generationResult.SuggestedDescription,
                contextChunkStrings,
                tokenUsage != null 
                    ? new GenerationTokenUsage(tokenUsage.PromptTokens, tokenUsage.CompletionTokens, tokenUsage.TotalTokens)
                    : null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Failed to generate entity fields for template '{TemplateName}' ({TemplateId})",
                template.DisplayName, template.Id);

            return EntityGenerationResult.Failed($"Generation failed: {ex.Message}");
        }
    }

    /// <inheritdoc />
    public async Task<EntityImageGenerationResult> GenerateEntityImageAsync(
        LoreEntity entity,
        EntityTemplate template,
        string? style = null,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Generating image for entity '{EntityName}' ({EntityId}), Template: {TemplateId}",
            entity.Name, entity.Id, template.Id);

        try
        {
            // Build image prompt from entity data
            // Note: GameSystemId comes from template since entity may not have campaign loaded
            var imagePrompt = await BuildImagePromptAsync(
                entity, 
                template, 
                template.GameSystemId, 
                style, 
                cancellationToken);

            _logger.LogDebug("Generated image prompt: {ImagePrompt}", imagePrompt);

            // Generate the image
            var imageResult = await _aiService.GenerateImageAsync(
                imagePrompt,
                style ?? "fantasy",
                aspectRatio: "1:1", // Square for avatars
                negativePrompt: "blurry, low quality, distorted, ugly, deformed",
                cancellationToken);

            if (!imageResult.Success || string.IsNullOrEmpty(imageResult.ImageBase64))
            {
                return EntityImageGenerationResult.Failed(
                    "Image generation failed - no image data returned");
            }

            return EntityImageGenerationResult.Successful(
                imageResult.ImageBase64,
                storedImageUrl: imageResult.ImageUrl, // May be null if not auto-stored
                generatedPrompt: imagePrompt);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Failed to generate image for entity '{EntityName}' ({EntityId})",
                entity.Name, entity.Id);

            return EntityImageGenerationResult.Failed($"Image generation failed: {ex.Message}");
        }
    }

    /// <inheritdoc />
    public Task<EntityImageGenerationResult> RegenerateEntityImageAsync(
        LoreEntity entity,
        EntityTemplate template,
        string? style = null,
        CancellationToken cancellationToken = default)
    {
        // For now, regeneration uses the same logic as generation
        // In the future, this could preserve previous prompt history or use different parameters
        _logger.LogInformation(
            "Regenerating image for entity '{EntityName}' ({EntityId})",
            entity.Name, entity.Id);

        return GenerateEntityImageAsync(entity, template, style, cancellationToken);
    }

    /// <summary>
    /// Builds the system prompt for field generation.
    /// </summary>
    private static string BuildFieldGenerationSystemPrompt(
        EntityTemplate template,
        IReadOnlyList<FieldDefinition> fieldDefinitions)
    {
        var fieldDescriptions = fieldDefinitions
            .OrderBy(f => f.Order)
            .Select(f => FormatFieldDescription(f))
            .ToList();

        return $"""
            You are an expert tabletop RPG content creator specializing in generating entity data.
            You are generating a {template.DisplayName} ({template.EntityTypeName}) for a tabletop RPG game.
            
            {(string.IsNullOrEmpty(template.Description) ? "" : $"Entity Description: {template.Description}")}
            
            You must generate values for the following fields:
            {string.Join("\n", fieldDescriptions)}
            
            IMPORTANT RULES:
            1. Output ONLY valid minified JSON - no explanations or markdown.
            2. Include all required fields.
            3. Follow the data type specified for each field.
            4. For Select fields, use ONLY the provided options.
            5. For Number fields, respect min/max constraints.
            6. Be creative but consistent with the game system rules provided in the context.
            7. Include "suggestedName" and "suggestedDescription" fields in your response.
            
            Your response must be a minified JSON object with field names as keys and generated values.
            """;
    }

    /// <summary>
    /// Builds the user prompt for field generation.
    /// </summary>
    private static string BuildFieldGenerationUserPrompt(
        EntityGenerationConfig config,
        IReadOnlyList<FieldDefinition> fieldDefinitions,
        IReadOnlyList<RagContextChunk> ragChunks)
    {
        var prompt = new List<string>();

        // Add user's prompt/context if provided
        if (!string.IsNullOrWhiteSpace(config.UserPrompt))
        {
            prompt.Add($"User Request: {config.UserPrompt}");
            prompt.Add("");
        }

        // Add existing values that should be preserved
        if (config.ExistingValues.Any())
        {
            prompt.Add("Preserve these existing values:");
            foreach (var (key, value) in config.ExistingValues)
            {
                prompt.Add($"  - {key}: {value}");
            }
            prompt.Add("");
        }

        // Specify which fields to generate
        if (config.HasSpecificFields)
        {
            prompt.Add($"Generate values for these specific fields: {string.Join(", ", config.FieldsToGenerate)}");
        }
        else
        {
            prompt.Add("Generate values for all template fields.");
        }

        // Add RAG context if available
        if (ragChunks.Any())
        {
            prompt.Add("");
            prompt.Add("Reference the following game system rules and lore:");
            foreach (var chunk in ragChunks)
            {
                prompt.Add($"--- From '{chunk.SourceTitle}' ---");
                prompt.Add(chunk.Content);
                prompt.Add("");
            }
        }

        prompt.Add("");
        prompt.Add("Generate the JSON response now:");

        return string.Join("\n", prompt);
    }

    /// <summary>
    /// Formats a field definition for the system prompt.
    /// </summary>
    private static string FormatFieldDescription(FieldDefinition field)
    {
        var parts = new List<string>
        {
            $"- {field.Name} ({field.DisplayName}): {field.FieldType}"
        };

        if (field.IsRequired)
            parts.Add("REQUIRED");

        if (!string.IsNullOrEmpty(field.Description))
            parts.Add($"- {field.Description}");

        if (field.FieldType == FieldType.Select || field.FieldType == FieldType.MultiSelect)
        {
            var options = field.GetOptions();
            if (options.Any())
                parts.Add($"Options: [{string.Join(", ", options)}]");
        }

        if (field.MinValue.HasValue || field.MaxValue.HasValue)
        {
            var constraints = new List<string>();
            if (field.MinValue.HasValue) constraints.Add($"min: {field.MinValue}");
            if (field.MaxValue.HasValue) constraints.Add($"max: {field.MaxValue}");
            parts.Add($"Constraints: {string.Join(", ", constraints)}");
        }

        return string.Join(" ", parts);
    }

    /// <summary>
    /// Parses the AI-generated JSON response into field values.
    /// </summary>
    private EntityGenerationResult ParseGeneratedFields(
        string jsonResponse,
        IReadOnlyList<FieldDefinition> fieldDefinitions,
        EntityGenerationConfig config)
    {
        try
        {
            // Clean the response - remove any markdown code blocks
            var cleanedJson = CleanJsonResponse(jsonResponse);

            // Parse the JSON
            using var document = JsonDocument.Parse(cleanedJson);
            var root = document.RootElement;

            var generatedFields = new Dictionary<string, object?>();
            string? suggestedName = null;
            string? suggestedDescription = null;

            // Extract special fields
            if (root.TryGetProperty("suggestedName", out var nameElement))
                suggestedName = nameElement.GetString();
            
            if (root.TryGetProperty("suggestedDescription", out var descElement))
                suggestedDescription = descElement.GetString();

            // Extract field values
            foreach (var field in fieldDefinitions)
            {
                // Skip if not supposed to generate this field
                if (!config.ShouldGenerateField(field.Name))
                    continue;

                // Check if field exists in response
                if (!root.TryGetProperty(field.Name, out var valueElement))
                {
                    // Use existing value if available
                    var existingValue = config.GetExistingValue(field.Name);
                    if (existingValue != null)
                    {
                        generatedFields[field.Name] = existingValue;
                    }
                    else if (field.IsRequired)
                    {
                        _logger.LogWarning("Required field '{FieldName}' not found in generated response", field.Name);
                    }
                    continue;
                }

                // Convert value based on field type
                var convertedValue = ConvertJsonValue(valueElement, field.FieldType);
                generatedFields[field.Name] = convertedValue;
            }

            return EntityGenerationResult.Successful(
                generatedFields,
                suggestedName,
                suggestedDescription);
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to parse generated JSON: {Response}", jsonResponse);
            return EntityGenerationResult.Failed($"Failed to parse AI response as JSON: {ex.Message}");
        }
    }

    /// <summary>
    /// Cleans JSON response by removing markdown code blocks.
    /// </summary>
    private static string CleanJsonResponse(string response)
    {
        return Loremaster.Shared.Helpers.JsonSanitizationHelper.StripMarkdownCodeFences(response);
    }

    /// <summary>
    /// Converts a JSON element to the appropriate type based on field type.
    /// </summary>
    private static object? ConvertJsonValue(JsonElement element, FieldType fieldType)
    {
        if (element.ValueKind == JsonValueKind.Null)
            return null;

        return fieldType switch
        {
            FieldType.Text or FieldType.TextArea => element.GetString(),
            FieldType.Number => element.ValueKind == JsonValueKind.Number 
                ? element.GetDecimal() 
                : decimal.TryParse(element.GetString(), out var d) ? d : 0m,
            FieldType.Boolean => element.ValueKind == JsonValueKind.True || 
                                 element.ValueKind == JsonValueKind.False 
                ? element.GetBoolean() 
                : bool.TryParse(element.GetString(), out var b) && b,
            FieldType.Select => element.GetString(),
            FieldType.MultiSelect => element.ValueKind == JsonValueKind.Array
                ? element.EnumerateArray().Select(e => e.GetString()).Where(s => s != null).ToList()
                : new List<string?>(),
            _ => element.GetString()
        };
    }

    /// <summary>
    /// Builds an image generation prompt from entity data.
    /// </summary>
    private async Task<string> BuildImagePromptAsync(
        LoreEntity entity,
        EntityTemplate template,
        Guid gameSystemId,
        string? style,
        CancellationToken cancellationToken)
    {
        var promptParts = new List<string>();

        // Base description
        promptParts.Add($"A {template.DisplayName.ToLowerInvariant()}");

        // Add entity name if meaningful
        if (!string.IsNullOrWhiteSpace(entity.Name) && entity.Name != "Unnamed")
        {
            promptParts.Add($"named {entity.Name}");
        }

        // Add entity description
        if (!string.IsNullOrWhiteSpace(entity.Description))
        {
            promptParts.Add($"described as: {entity.Description}");
        }

        // Extract visual attributes from entity
        var visualAttributes = ExtractVisualAttributes(entity, template);
        if (visualAttributes.Any())
        {
            promptParts.Add($"with {string.Join(", ", visualAttributes)}");
        }

        // Try to get style context from RAG
        var styleSearchContext = BuildStyleSearchContext(entity, template, visualAttributes);
        var styleChunks = await _ragContextProvider.GetStyleContextAsync(
            gameSystemId,
            entity.OwnerId,
            template.EntityTypeName,
            styleSearchContext,
            cancellationToken);

        if (styleChunks.Any())
        {
            // Extract key visual descriptors from style context
            var styleHints = styleChunks
                .Select(c => c.Content)
                .Take(1) // Just use the most relevant chunk
                .FirstOrDefault();

            if (!string.IsNullOrEmpty(styleHints) && styleHints.Length <= 200)
            {
                promptParts.Add($"in the style of: {styleHints}");
            }
        }

        // Add style hint
        var styleDescription = style?.ToLowerInvariant() switch
        {
            "realistic" => "photorealistic, highly detailed",
            "anime" => "anime style, vibrant colors",
            "sketch" => "pencil sketch, black and white",
            "fantasy" or _ => "fantasy art style, detailed illustration"
        };
        promptParts.Add(styleDescription);

        // Combine and ensure reasonable length
        var fullPrompt = string.Join(", ", promptParts);
        
        // Truncate if too long (most image APIs have limits)
        if (fullPrompt.Length > 500)
        {
            fullPrompt = fullPrompt[..497] + "...";
        }

        return fullPrompt;
    }

    /// <summary>
    /// Builds a context string for style search based on entity attributes.
    /// </summary>
    private static string? BuildStyleSearchContext(LoreEntity entity, EntityTemplate template, List<string> visualAttributes)
    {
        var contextParts = new List<string>();
        
        if (!string.IsNullOrWhiteSpace(entity.Description))
        {
            var truncatedDesc = entity.Description.Length > 100 
                ? entity.Description[..100] 
                : entity.Description;
            contextParts.Add(truncatedDesc);
        }
        
        if (visualAttributes.Any())
        {
            contextParts.AddRange(visualAttributes.Take(3));
        }
        
        return contextParts.Any() ? string.Join(", ", contextParts) : null;
    }

    /// <summary>
    /// Extracts visual attributes from entity data based on template fields.
    /// </summary>
    private static List<string> ExtractVisualAttributes(LoreEntity entity, EntityTemplate template)
    {
        var visualKeywords = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "appearance", "hair", "eye", "eyes", "skin", "height", "build",
            "clothing", "armor", "weapon", "color", "race", "species"
        };

        var attributes = new List<string>();
        var entityAttributes = entity.GetAttributes();
        var fieldDefinitions = template.GetFieldDefinitions();

        foreach (var field in fieldDefinitions)
        {
            // Check if this field is visual
            var isVisual = visualKeywords.Any(k => 
                field.Name.Contains(k, StringComparison.OrdinalIgnoreCase) ||
                field.DisplayName.Contains(k, StringComparison.OrdinalIgnoreCase));

            if (!isVisual) continue;

            // Try to get the value
            if (entityAttributes.TryGetValue(field.Name, out var value) && value != null)
            {
                var valueStr = value.ToString();
                if (!string.IsNullOrWhiteSpace(valueStr))
                {
                    attributes.Add($"{field.DisplayName}: {valueStr}");
                }
            }
        }

        return attributes;
    }
}
