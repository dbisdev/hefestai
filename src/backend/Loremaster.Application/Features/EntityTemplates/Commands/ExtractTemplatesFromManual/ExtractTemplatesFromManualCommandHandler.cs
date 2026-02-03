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
    /// </summary>
    private const string EntityTypeExtractionPrompt = @"
You are analyzing a tabletop RPG rulebook to identify the entity types (character types, creature types, vehicle types, item types, location types, etc.) that players can create in this game system.

For each entity type found, extract:
1. The entity type name (e.g., ""Character"", ""Vehicle"", ""Starship"", ""NPC"", ""Monster"")
2. A display name (human-readable)
3. A brief description
4. The fields/attributes that define this entity type (name, type, required, description)

Respond with a JSON array of entity types in this exact format:
```json
[
  {
    ""entityTypeName"": ""character"",
    ""displayName"": ""Player Character"",
    ""description"": ""A player-controlled character in the game"",
    ""iconHint"": ""user"",
    ""fields"": [
      {
        ""name"": ""name"",
        ""displayName"": ""Character Name"",
        ""fieldType"": ""Text"",
        ""isRequired"": true,
        ""description"": ""The character's name""
      },
      {
        ""name"": ""level"",
        ""displayName"": ""Level"",
        ""fieldType"": ""Number"",
        ""isRequired"": true,
        ""minValue"": 1,
        ""maxValue"": 20
      }
    ]
  }
]
```

Field types must be one of: Text, TextArea, Number, Boolean, Select, MultiSelect, Date, Url, Json

Only include entity types that are explicitly defined in the rulebook with clear creation rules.
Do not invent entity types that are not in the source material.
";

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

        // Get relevant document content via semantic search
        var searchQuery = "entity types character creation rules attributes stats fields";
        var queryEmbedding = await _embeddingService.GetEmbeddingAsync(searchQuery, cancellationToken);
        
        var searchResults = await _documentRepository.SemanticSearchAsync(
            queryEmbedding,
            request.OwnerId,
            limit: 10,
            threshold: 0.5f,
            gameSystemId: request.GameSystemId,
            cancellationToken: cancellationToken);

        if (!searchResults.Any())
        {
            _logger.LogWarning(
                "No documents found for game system {GameSystemId}. Upload manuals first.",
                request.GameSystemId);
            
            return new ExtractTemplatesResult(
                0, 0, 0, 
                Array.Empty<ExtractedTemplateInfo>(),
                "No manuals found for this game system. Please upload manuals first.");
        }

        // Build context from search results
        var context = searchResults
            .Select(r => r.Document.Content)
            .ToList();

        // Use RAG to extract entity types
        var ragResult = await _embeddingService.GenerateWithContextAsync(
            EntityTypeExtractionPrompt,
            context,
            "You are a game system analyst extracting structured data from rulebooks.",
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

        // Get source document ID (first document if not specified)
        var sourceDocumentId = request.SourceDocumentId ?? searchResults.First().Document.Id;

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

    private static List<FieldDefinition> ConvertToFieldDefinitions(List<ExtractedField> fields)
    {
        return fields.Select((f, index) => FieldDefinition.Create(
            f.Name,
            f.DisplayName,
            ParseFieldType(f.FieldType),
            f.IsRequired,
            f.DefaultValue,
            f.Description,
            f.Order ?? index,
            f.Options,
            f.MinValue,
            f.MaxValue,
            f.ValidationPattern
        )).ToList();
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
