using System.Text.Json;
using Loremaster.Application.Common.Interfaces;
using Loremaster.Domain.Entities;
using Loremaster.Domain.Enums;
using Loremaster.Domain.ValueObjects;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Loremaster.Application.Features.EntityTemplates.Commands.ExtractTemplatesFromManual;

/// <summary>
/// Handler for ExtractTemplatesFromManualCommand.
/// Uses RAG to analyze manuals and extract entity type definitions.
/// </summary>
public class ExtractTemplatesFromManualCommandHandler 
    : IRequestHandler<ExtractTemplatesFromManualCommand, ExtractTemplatesResult>
{
    private readonly IEntityTemplateRepository _templateRepository;
    private readonly IGameSystemRepository _gameSystemRepository;
    private readonly IDocumentRepository _documentRepository;
    private readonly IEmbeddingService _embeddingService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<ExtractTemplatesFromManualCommandHandler> _logger;

    /// <summary>
    /// Prompt for RAG to extract entity types from manuals.
    /// NOTE: Genkit limit is 2000 chars for query - keep this concise!
    /// </summary>
    private const string EntityTypeExtractionPrompt = @"Extract ALL entity types from this RPG rulebook (characters, NPCs, vehicles, starships, monsters, locations, items).
Important search ATTRIBUTES and based or dependant SKILLS.
Return JSON array with nested field structure:
```json
[{
  ""entityTypeName"": ""player_character"",
  ""displayName"": ""Player Character"",
  ""description"": ""Playable character"",
  ""schema"": {
    ""attributes"": {
      ""strength"": {""name"":""Strength"",""description"":""Physical power"",""range"":{""min"":1,""max"":5}},
      ""agility"": {""name"":""Agility"",""description"":""Speed and reflexes"",""range"":{""min"":1,""max"":5}}
    },
    ""skills"": {
      ""combat"": {""attribute"":""strength"",""description"":""Fighting ability""},
      ""piloting"": {""attribute"":""agility"",""description"":""Vehicle control""}
    },
    ""derived_stats"": {
      ""health"": {""formula"":""strength*2"",""description"":""Hit points""}
    },
    ""identity"": {
      ""name"":""string"",""career"":""string"",""appearance"":""string""
    },
    ""gear"": {""items"":""array"",""cash"":""number""}
  }
}]
```
Include ALL attributes, skills, derived stats, identity fields, gear from the rulebook.";

    /// <summary>
    /// Search queries to find different types of entities in manuals.
    /// </summary>
    private static readonly string[] EntitySearchQueries = new[]
    {
        "player character creation stats attributes abilities skills life path resolve stress",
        "npc non-player character enemies allies contacts",
        "vehicle car bike aircraft stats speed armor starship spacecraft ship hull weapons shields",
        "monster creature beast alien stats combat",
        "location place planet system city base settlement star base station",
        "equipment weapons armor gear items cybernetics"
    };

    public ExtractTemplatesFromManualCommandHandler(
        IEntityTemplateRepository templateRepository,
        IGameSystemRepository gameSystemRepository,
        IDocumentRepository documentRepository,
        IEmbeddingService embeddingService,
        IUnitOfWork unitOfWork,
        ILogger<ExtractTemplatesFromManualCommandHandler> logger)
    {
        _templateRepository = templateRepository;
        _gameSystemRepository = gameSystemRepository;
        _documentRepository = documentRepository;
        _embeddingService = embeddingService;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<ExtractTemplatesResult> Handle(
        ExtractTemplatesFromManualCommand request, 
        CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Extracting templates from manuals for game system {GameSystemId}",
            request.GameSystemId);

        // Verify game system exists
        var gameSystem = await _gameSystemRepository.GetByIdAsync(
            request.GameSystemId, cancellationToken);
        
        if (gameSystem == null)
        {
            throw new ArgumentException($"Game system with ID {request.GameSystemId} not found");
        }

        // Verify user is Admin or owner of the game system
        var isOwner = gameSystem.OwnerId == request.OwnerId;
        if (!request.IsAdmin && !isOwner)
        {
            throw new UnauthorizedAccessException(
                $"You don't have permission to extract templates from this game system. " +
                $"Only the owner or an Admin can perform this action.");
        }

        // Perform multiple semantic searches to capture different entity types
        var allResults = new Dictionary<Guid, DocumentSearchResult>();
        Guid? firstDocumentId = null;
        
        foreach (var searchQuery in EntitySearchQueries)
        {
            var queryEmbedding = await _embeddingService.GetEmbeddingAsync(searchQuery, cancellationToken);
            
            // Admin users and system owners can search across all owners' documents for the game system
            var searchResults = await _documentRepository.SemanticSearchAsync(
                queryEmbedding,
                request.CurrentUserId,
                limit: 8,  // 7 results per query category
                threshold: 0.4f,  // Lower threshold to capture more content
                gameSystemId: request.GameSystemId,
                skipOwnerFilter: request.IsAdmin || request.IsSystemOwner,
                cancellationToken: cancellationToken);

            foreach (var result in searchResults)
            {
                // Use dictionary to deduplicate by document ID
                if (!allResults.ContainsKey(result.Document.Id))
                {
                    allResults[result.Document.Id] = result;
                    firstDocumentId ??= result.Document.Id;
                }
            }
        }

        if (!allResults.Any())
        {
            _logger.LogWarning(
                "No documents found for game system {GameSystemId}. Upload manuals first.",
                request.GameSystemId);
            
            return new ExtractTemplatesResult(
                0, 0, 0, 
                Array.Empty<ExtractedTemplateInfo>(),
                "No manuals found for this game system. Please upload manuals first.");
        }

        _logger.LogInformation(
            "Found {ChunkCount} unique document chunks for entity extraction (IsAdmin: {IsAdmin}, IsSystemOwner: {IsSystemOwner})",
            allResults.Count, request.IsAdmin, request.IsSystemOwner);

        // Build context from search results, sorted by similarity
        // Genkit API limit: max 10 context items
        var context = allResults.Values
            .OrderByDescending(r => r.SimilarityScore)
            .Take(10)
            .Select(r => r.Document.Content)
            .ToList();

        // Use RAG to extract entity types
        var ragResult = await _embeddingService.GenerateWithContextAsync(
            EntityTypeExtractionPrompt,
            context,
            "You are a game system analyst. Extract ALL entity types: characters, NPCs, vehicles, starships, monsters, locations, items. Respond only with valid minified JSON, no markdown code fences.",
            temperature: 0.3f,
            maxTokens: 8192,
            cancellationToken: cancellationToken);

        // Parse the RAG response
        var extractedTypes = ParseExtractedTypes(ragResult.Answer);
        
        if (!extractedTypes.Any())
        {
            _logger.LogWarning("No entity types could be extracted from the manuals");
            return new ExtractTemplatesResult(
                0, 0, 0,
                Array.Empty<ExtractedTemplateInfo>(),
                "Could not extract entity types from the manuals. The content may not contain clear entity definitions.");
        }

        _logger.LogInformation(
            "Extracted {EntityTypeCount} entity types from manuals",
            extractedTypes.Count);

        // Get source document ID (first document if not specified)
        var sourceDocumentId = request.SourceDocumentId ?? firstDocumentId ?? Guid.Empty;

        // Process extracted types
        var results = new List<ExtractedTemplateInfo>();
        var created = 0;
        var updated = 0;
        var skipped = 0;

        foreach (var extractedType in extractedTypes)
        {
            try
            {
                var (template, isNew, wasSkipped, extractedFields) = await CreateOrUpdateTemplateAsync(
                    extractedType,
                    request.GameSystemId,
                    request.OwnerId,
                    sourceDocumentId,
                    cancellationToken);

                if (wasSkipped)
                {
                    // Template was skipped because confirmed version exists
                    // But we still return the extracted fields so user can compare
                    skipped++;
                    results.Add(new ExtractedTemplateInfo(
                        template.Id,
                        template.EntityTypeName,
                        template.DisplayName,
                        extractedFields?.Count ?? template.GetFieldDefinitions().Count,
                        false,
                        $"Already confirmed. New extraction has {extractedFields?.Count ?? 0} fields.",
                        extractedFields));
                }
                else
                {
                    results.Add(new ExtractedTemplateInfo(
                        template.Id,
                        template.EntityTypeName,
                        template.DisplayName,
                        template.GetFieldDefinitions().Count,
                        isNew,
                        null,
                        null));

                    if (isNew) created++;
                    else updated++;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, 
                    "Failed to create template for entity type {EntityType}", 
                    extractedType.EntityTypeName);
                
                skipped++;
                results.Add(new ExtractedTemplateInfo(
                    Guid.Empty,
                    extractedType.EntityTypeName,
                    extractedType.DisplayName,
                    0,
                    false,
                    ex.Message,
                    null));
            }
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Template extraction complete: {Created} created, {Updated} updated, {Skipped} skipped",
            created, updated, skipped);

        return new ExtractTemplatesResult(created, updated, skipped, results);
    }

    /// <summary>
    /// Creates or updates a template based on extracted data.
    /// Returns: (Template, IsNew, WasSkipped, ExtractedFields if skipped)
    /// </summary>
    private async Task<(EntityTemplate Template, bool IsNew, bool WasSkipped, List<FieldDefinition>? ExtractedFields)> CreateOrUpdateTemplateAsync(
        ExtractedEntityType extractedType,
        Guid gameSystemId,
        Guid ownerId,
        Guid sourceDocumentId,
        CancellationToken cancellationToken)
    {
        var normalizedName = EntityTemplate.NormalizeEntityTypeName(extractedType.EntityTypeName);
        
        // Convert fields first (we might need them even if template exists)
        var extractedFields = ConvertToFieldDefinitions(extractedType.Fields);
        
        // Check if confirmed template already exists (owned by this owner or by an Admin)
        var existingTemplate = await _templateRepository.GetConfirmedTemplateForEntityTypeAsync(
            gameSystemId, ownerId, normalizedName, cancellationToken);

        // If confirmed template exists, return it with extracted fields for comparison
        if (existingTemplate != null)
        {
            _logger.LogInformation(
                "Skipping extraction for '{EntityType}' - confirmed template already exists. Extracted {FieldCount} fields for comparison.",
                normalizedName, extractedFields.Count);
            
            // Return the existing template but also the extracted fields
            return (existingTemplate, false, true, extractedFields);
        }

        // Check for draft/pending template
        var templates = await _templateRepository.GetByGameSystemIdAsync(
            gameSystemId, ownerId, cancellationToken);
        var draftTemplate = templates.FirstOrDefault(t => 
            t.EntityTypeName == normalizedName && t.Status != TemplateStatus.Confirmed);

        if (draftTemplate != null)
        {
            // Update existing draft
            draftTemplate.Update(
                extractedType.DisplayName,
                extractedType.Description,
                extractedType.IconHint);
            
            draftTemplate.SetFieldDefinitions(extractedFields);
            
            _templateRepository.Update(draftTemplate);
            return (draftTemplate, false, false, null);
        }

        // Create new template
        var template = EntityTemplate.Create(
            extractedType.EntityTypeName,
            extractedType.DisplayName,
            gameSystemId,
            ownerId,
            extractedType.Description,
            sourceDocumentId,
            iconHint: extractedType.IconHint);

        template.SetFieldDefinitions(extractedFields);

        await _templateRepository.AddAsync(template, cancellationToken);
        return (template, true, false, null);
    }

    /// <summary>
    /// Converts extracted fields to domain FieldDefinition objects.
    /// Handles edge cases like Select fields without options by converting them to Text.
    /// Sanitizes field names to ensure they are valid identifiers.
    /// </summary>
    private static List<FieldDefinition> ConvertToFieldDefinitions(List<ExtractedField> fields)
    {
        var usedNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        
        return fields.Select((f, index) => 
        {
            var fieldType = ParseFieldType(f.FieldType);
            var options = f.Options;
            
            // If Select/MultiSelect but no options provided, convert to Text field
            if ((fieldType == FieldType.Select || fieldType == FieldType.MultiSelect) 
                && (options == null || !options.Any()))
            {
                fieldType = FieldType.Text;
                options = null;
            }
            
            // Sanitize field name to be a valid identifier
            var sanitizedName = SanitizeFieldName(f.Name);
            
            // Ensure unique name
            var uniqueName = sanitizedName;
            var counter = 1;
            while (usedNames.Contains(uniqueName))
            {
                uniqueName = $"{sanitizedName}_{counter++}";
            }
            usedNames.Add(uniqueName);
            
            // Use original name as display name if not provided
            var displayName = string.IsNullOrWhiteSpace(f.DisplayName) ? f.Name : f.DisplayName;
            
            return FieldDefinition.Create(
                uniqueName,
                displayName,
                fieldType,
                f.IsRequired,
                f.DefaultValue,
                f.Description,
                f.Order ?? index,
                options,
                f.MinValue,
                f.MaxValue,
                f.ValidationPattern
            );
        }).ToList();
    }

    /// <summary>
    /// Sanitizes a field name to be a valid identifier.
    /// - Replaces spaces and hyphens with underscores
    /// - Removes invalid characters
    /// - Ensures it starts with a letter
    /// - Converts to snake_case
    /// </summary>
    private static string SanitizeFieldName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return "field";
        
        // Convert to lowercase and replace common separators with underscores
        var sanitized = name.Trim()
            .ToLowerInvariant()
            .Replace(' ', '_')
            .Replace('-', '_')
            .Replace('.', '_');
        
        // Remove any character that's not a letter, digit, or underscore
        sanitized = new string(sanitized
            .Where(c => char.IsLetterOrDigit(c) || c == '_')
            .ToArray());
        
        // Remove consecutive underscores
        while (sanitized.Contains("__"))
            sanitized = sanitized.Replace("__", "_");
        
        // Trim underscores from start and end
        sanitized = sanitized.Trim('_');
        
        // If empty after sanitization, use default
        if (string.IsNullOrEmpty(sanitized))
            return "field";
        
        // Ensure it starts with a letter
        if (!char.IsLetter(sanitized[0]))
            sanitized = "f_" + sanitized;
        
        return sanitized;
    }

    private static FieldType ParseFieldType(string fieldType)
    {
        return fieldType.ToLowerInvariant() switch
        {
            "text" => FieldType.Text,
            "textarea" => FieldType.TextArea,
            "number" => FieldType.Number,
            "boolean" or "bool" => FieldType.Boolean,
            "select" => FieldType.Select,
            "multiselect" => FieldType.MultiSelect,
            "date" => FieldType.Date,
            "url" => FieldType.Url,
            "json" => FieldType.Json,
            _ => FieldType.Text
        };
    }

    private List<ExtractedEntityType> ParseExtractedTypes(string ragResponse)
    {
        try
        {
            // Find JSON array in response
            var jsonStart = ragResponse.IndexOf('[');
            var jsonEnd = ragResponse.LastIndexOf(']');
            
            if (jsonStart < 0 || jsonEnd < 0 || jsonEnd <= jsonStart)
            {
                _logger.LogWarning("Could not find JSON array in RAG response");
                return new List<ExtractedEntityType>();
            }

            var jsonContent = ragResponse.Substring(jsonStart, jsonEnd - jsonStart + 1);
            
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };
            
            var extracted = JsonSerializer.Deserialize<List<ExtractedEntityType>>(jsonContent, options);
            
            // Convert schema to fields if present
            if (extracted != null)
            {
                foreach (var entityType in extracted)
                {
                    if (entityType.Schema.HasValue && !entityType.Fields.Any())
                    {
                        entityType.Fields = FlattenSchemaToFields(entityType.Schema.Value);
                    }
                }
            }
            
            return extracted ?? new List<ExtractedEntityType>();
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "Failed to parse RAG response as JSON, attempting to extract partial data");
            
            // Try to extract individual entity type objects from the response
            return TryExtractPartialData(ragResponse);
        }
    }

    /// <summary>
    /// Attempts to extract entity types from a malformed JSON response.
    /// </summary>
    private List<ExtractedEntityType> TryExtractPartialData(string ragResponse)
    {
        var result = new List<ExtractedEntityType>();
        
        try
        {
            // Look for patterns like {"entityTypeName": "...", "displayName": "..."}
            var regex = new System.Text.RegularExpressions.Regex(
                @"\{[^{}]*""entityTypeName""\s*:\s*""([^""]+)""[^{}]*""displayName""\s*:\s*""([^""]+)""[^{}]*\}",
                System.Text.RegularExpressions.RegexOptions.IgnoreCase);

            var matches = regex.Matches(ragResponse);
            
            foreach (System.Text.RegularExpressions.Match match in matches)
            {
                if (match.Groups.Count >= 3)
                {
                    result.Add(new ExtractedEntityType
                    {
                        EntityTypeName = match.Groups[1].Value,
                        DisplayName = match.Groups[2].Value,
                        Fields = new List<ExtractedField>()
                    });
                }
            }

            if (result.Any())
            {
                _logger.LogInformation("Extracted {Count} entity types using fallback parsing", result.Count);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Fallback parsing also failed");
        }
        
        return result;
    }

    /// <summary>
    /// Flattens a nested schema object into a list of field definitions.
    /// Handles structures like: attributes, skills, derived_stats, identity, gear, etc.
    /// </summary>
    private List<ExtractedField> FlattenSchemaToFields(JsonElement schema)
    {
        var fields = new List<ExtractedField>();
        var order = 0;

        foreach (var section in schema.EnumerateObject())
        {
            var sectionName = section.Name; // e.g., "attributes", "skills", "identity"
            
            if (section.Value.ValueKind == JsonValueKind.Object)
            {
                fields.AddRange(ProcessSchemaSection(sectionName, section.Value, ref order));
            }
        }

        return fields;
    }

    /// <summary>
    /// Processes a section of the schema (e.g., attributes, skills).
    /// </summary>
    private List<ExtractedField> ProcessSchemaSection(string sectionName, JsonElement section, ref int order)
    {
        var fields = new List<ExtractedField>();

        foreach (var item in section.EnumerateObject())
        {
            var fieldName = item.Name;
            var fieldValue = item.Value;
            
            // Create field name with section prefix for grouping
            var fullFieldName = $"{sectionName}_{fieldName}";
            
            var field = new ExtractedField
            {
                Name = fullFieldName,
                Order = order++
            };

            if (fieldValue.ValueKind == JsonValueKind.String)
            {
                // Simple type declaration like "name": "string"
                var typeStr = fieldValue.GetString() ?? "string";
                field.DisplayName = FormatDisplayName(fieldName);
                field.FieldType = InferFieldTypeFromString(typeStr);
                field.Description = $"{sectionName}: {fieldName}";
            }
            else if (fieldValue.ValueKind == JsonValueKind.Object)
            {
                // Complex field with properties
                field.DisplayName = GetJsonString(fieldValue, "name") ?? FormatDisplayName(fieldName);
                field.Description = GetJsonString(fieldValue, "description");
                
                // Check for range (min/max)
                if (fieldValue.TryGetProperty("range", out var range))
                {
                    field.FieldType = "Number";
                    field.MinValue = GetJsonDecimal(range, "min");
                    field.MaxValue = GetJsonDecimal(range, "max");
                }
                // Check for formula (derived stats)
                else if (fieldValue.TryGetProperty("formula", out _))
                {
                    field.FieldType = "Text";
                    field.Description = $"{field.Description} (Formula: {GetJsonString(fieldValue, "formula")})";
                }
                // Check for attribute reference (skills)
                else if (fieldValue.TryGetProperty("attribute", out var attr))
                {
                    field.FieldType = "Number";
                    field.Description = $"{field.Description} (Based on: {attr.GetString()})";
                    field.MinValue = 0;
                    field.MaxValue = 5;
                }
                // Check for type hint
                else if (fieldValue.TryGetProperty("type", out var typeHint))
                {
                    field.FieldType = InferFieldTypeFromString(typeHint.GetString() ?? "text");
                }
                else
                {
                    // Default to Text for complex objects
                    field.FieldType = "Text";
                }
            }
            else if (fieldValue.ValueKind == JsonValueKind.Array)
            {
                // Array type - use Json field
                field.DisplayName = FormatDisplayName(fieldName);
                field.FieldType = "Json";
                field.Description = $"{sectionName}: {fieldName} (list)";
            }
            else
            {
                // Primitive value
                field.DisplayName = FormatDisplayName(fieldName);
                field.FieldType = InferFieldTypeFromJsonKind(fieldValue.ValueKind);
            }

            fields.Add(field);
        }

        return fields;
    }

    /// <summary>
    /// Infers field type from a string type declaration.
    /// </summary>
    private static string InferFieldTypeFromString(string typeStr)
    {
        return typeStr.ToLowerInvariant() switch
        {
            "string" or "text" => "Text",
            "number" or "int" or "integer" or "float" or "decimal" => "Number",
            "boolean" or "bool" => "Boolean",
            "array" or "list" => "Json",
            "object" or "json" => "Json",
            "date" or "datetime" => "Date",
            _ when typeStr.Contains("|") => "Select", // enum-like: "a | b | c"
            _ => "Text"
        };
    }

    /// <summary>
    /// Infers field type from JSON value kind.
    /// </summary>
    private static string InferFieldTypeFromJsonKind(JsonValueKind kind)
    {
        return kind switch
        {
            JsonValueKind.Number => "Number",
            JsonValueKind.True or JsonValueKind.False => "Boolean",
            JsonValueKind.Array => "Json",
            JsonValueKind.Object => "Json",
            _ => "Text"
        };
    }

    /// <summary>
    /// Formats a snake_case name to Title Case display name.
    /// </summary>
    private static string FormatDisplayName(string name)
    {
        if (string.IsNullOrEmpty(name)) return name;
        
        // Replace underscores with spaces and title case
        var words = name.Replace('_', ' ').Split(' ');
        return string.Join(" ", words.Select(w => 
            string.IsNullOrEmpty(w) ? w : char.ToUpper(w[0]) + w.Substring(1).ToLower()));
    }

    /// <summary>
    /// Safely gets a string property from a JsonElement.
    /// </summary>
    private static string? GetJsonString(JsonElement element, string propertyName)
    {
        if (element.TryGetProperty(propertyName, out var prop) && prop.ValueKind == JsonValueKind.String)
            return prop.GetString();
        return null;
    }

    /// <summary>
    /// Safely gets a decimal property from a JsonElement.
    /// </summary>
    private static decimal? GetJsonDecimal(JsonElement element, string propertyName)
    {
        if (element.TryGetProperty(propertyName, out var prop) && prop.ValueKind == JsonValueKind.Number)
            return prop.GetDecimal();
        return null;
    }

    /// <summary>
    /// Internal DTO for parsing RAG response.
    /// Supports both flat 'fields' array and nested 'schema' object.
    /// </summary>
    private class ExtractedEntityType
    {
        public string EntityTypeName { get; set; } = "";
        public string DisplayName { get; set; } = "";
        public string? Description { get; set; }
        public string? IconHint { get; set; }
        public List<ExtractedField> Fields { get; set; } = new();
        public JsonElement? Schema { get; set; }
    }

    private class ExtractedField
    {
        public string Name { get; set; } = "";
        public string DisplayName { get; set; } = "";
        public string FieldType { get; set; } = "Text";
        public bool IsRequired { get; set; }
        public string? DefaultValue { get; set; }
        public string? Description { get; set; }
        public int? Order { get; set; }
        public List<string>? Options { get; set; }
        public decimal? MinValue { get; set; }
        public decimal? MaxValue { get; set; }
        public string? ValidationPattern { get; set; }
    }
}
