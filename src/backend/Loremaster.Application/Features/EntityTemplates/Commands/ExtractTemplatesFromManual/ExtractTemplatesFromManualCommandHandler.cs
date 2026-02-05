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
    private const string EntityTypeExtractionPrompt = @"Extract ALL entity types from this RPG rulebook. Look for:
- Characters (player/NPC)
- Vehicles, Starships
- Monsters, Creatures
- Locations, Places
- Items, Equipment

Return JSON array:
```json
[{
  ""entityTypeName"": ""player_character"",
  ""displayName"": ""Player Character"",
  ""description"": ""A player-controlled character"",
  ""iconHint"": ""person"",
  ""fields"": [{
    ""name"": ""strength"",
    ""displayName"": ""Strength"",
    ""fieldType"": ""Number"",
    ""isRequired"": true,
    ""minValue"": 1,
    ""maxValue"": 20
  }]
}]
```
Field types: Text, TextArea, Number, Boolean, Select, MultiSelect, Date, Url, Json
Include ALL stats/attributes for each entity type found.";

    /// <summary>
    /// Search queries to find different types of entities in manuals.
    /// </summary>
    private static readonly string[] EntitySearchQueries = new[]
    {
        "player character creation stats attributes abilities skills life path",
        "NPC non-player character enemies allies contacts",
        "vehicle car bike aircraft stats speed armor",
        "starship spacecraft ship hull weapons shields",
        "monster creature beast alien stats combat",
        "location place planet system city base settlement star",
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

        // Perform multiple semantic searches to capture different entity types
        var allResults = new Dictionary<Guid, DocumentSearchResult>();
        Guid? firstDocumentId = null;
        
        foreach (var searchQuery in EntitySearchQueries)
        {
            var queryEmbedding = await _embeddingService.GetEmbeddingAsync(searchQuery, cancellationToken);
            
            // Admin users can search across all owners' documents for the game system
            var searchResults = await _documentRepository.SemanticSearchAsync(
                queryEmbedding,
                request.OwnerId,
                limit: 7,  // 5 results per query category
                threshold: 0.4f,  // Lower threshold to capture more content
                gameSystemId: request.GameSystemId,
                skipOwnerFilter: request.IsAdmin,
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
            "Found {ChunkCount} unique document chunks for entity extraction",
            allResults.Count);

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
            "You are a game system analyst. Extract ALL entity types: characters, NPCs, vehicles, starships, monsters, locations, items.",
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
                var (template, isNew) = await CreateOrUpdateTemplateAsync(
                    extractedType,
                    request.GameSystemId,
                    request.OwnerId,
                    sourceDocumentId,
                    cancellationToken);

                results.Add(new ExtractedTemplateInfo(
                    template.Id,
                    template.EntityTypeName,
                    template.DisplayName,
                    template.GetFieldDefinitions().Count,
                    isNew,
                    null));

                if (isNew) created++;
                else updated++;
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
                    ex.Message));
            }
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Template extraction complete: {Created} created, {Updated} updated, {Skipped} skipped",
            created, updated, skipped);

        return new ExtractTemplatesResult(created, updated, skipped, results);
    }

    private async Task<(EntityTemplate Template, bool IsNew)> CreateOrUpdateTemplateAsync(
        ExtractedEntityType extractedType,
        Guid gameSystemId,
        Guid ownerId,
        Guid sourceDocumentId,
        CancellationToken cancellationToken)
    {
        var normalizedName = EntityTemplate.NormalizeEntityTypeName(extractedType.EntityTypeName);
        
        // Check if template already exists
        var existingTemplate = await _templateRepository.GetConfirmedTemplateForEntityTypeAsync(
            gameSystemId, ownerId, normalizedName, cancellationToken);

        // If confirmed template exists, skip it
        if (existingTemplate != null)
        {
            throw new InvalidOperationException(
                $"A confirmed template for '{normalizedName}' already exists");
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
            
            var fields = ConvertToFieldDefinitions(extractedType.Fields);
            draftTemplate.SetFieldDefinitions(fields);
            
            _templateRepository.Update(draftTemplate);
            return (draftTemplate, false);
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

        var newFields = ConvertToFieldDefinitions(extractedType.Fields);
        template.SetFieldDefinitions(newFields);

        await _templateRepository.AddAsync(template, cancellationToken);
        return (template, true);
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
            return extracted ?? new List<ExtractedEntityType>();
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "Failed to parse RAG response as JSON");
            return new List<ExtractedEntityType>();
        }
    }

    /// <summary>
    /// Internal DTO for parsing RAG response.
    /// </summary>
    private class ExtractedEntityType
    {
        public string EntityTypeName { get; set; } = "";
        public string DisplayName { get; set; } = "";
        public string? Description { get; set; }
        public string? IconHint { get; set; }
        public List<ExtractedField> Fields { get; set; } = new();
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
