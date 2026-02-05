using System.Text.Json;
using Loremaster.Domain.Common;
using Loremaster.Domain.Enums;
using Loremaster.Domain.ValueObjects;

namespace Loremaster.Domain.Entities;

/// <summary>
/// Entity template definition extracted from game system manuals.
/// Defines the schema (fields) for creating entities of a specific type.
/// Templates must be confirmed before they can be used for entity creation.
/// </summary>
public class EntityTemplate : AuditableEntity
{
    /// <summary>
    /// Name of the entity type (e.g., "Character", "Vehicle", "Location").
    /// Must be unique within a game system.
    /// </summary>
    public string EntityTypeName { get; private set; } = null!;
    
    /// <summary>
    /// Human-readable display name for the entity type.
    /// </summary>
    public string DisplayName { get; private set; } = null!;
    
    /// <summary>
    /// Description of this entity type extracted from the manual.
    /// </summary>
    public string? Description { get; private set; }
    
    /// <summary>
    /// Current status in the confirmation workflow.
    /// </summary>
    public TemplateStatus Status { get; private set; } = TemplateStatus.Draft;
    
    /// <summary>
    /// Field definitions stored as JSON.
    /// </summary>
    public string FieldDefinitionsJson { get; private set; } = "[]";
    
    /// <summary>
    /// Optional icon or category hint for UI display.
    /// </summary>
    public string? IconHint { get; private set; }
    
    /// <summary>
    /// Version identifier if the template was extracted from a specific manual version.
    /// </summary>
    public string? Version { get; private set; }
    
    /// <summary>
    /// Notes from the user during review/confirmation.
    /// </summary>
    public string? ReviewNotes { get; private set; }
    
    /// <summary>
    /// Timestamp when the template was confirmed.
    /// </summary>
    public DateTime? ConfirmedAt { get; private set; }
    
    /// <summary>
    /// User who confirmed the template.
    /// </summary>
    public Guid? ConfirmedByUserId { get; private set; }
    
    // Navigation properties
    
    /// <summary>
    /// The game system this template belongs to.
    /// </summary>
    public Guid GameSystemId { get; private set; }
    public GameSystem GameSystem { get; private set; } = null!;
    
    /// <summary>
    /// The source document (manual) this template was extracted from.
    /// </summary>
    public Guid? SourceDocumentId { get; private set; }
    public Document? SourceDocument { get; private set; }
    
    /// <summary>
    /// Owner (Master) who created/owns this template.
    /// </summary>
    public Guid OwnerId { get; private set; }
    public User Owner { get; private set; } = null!;

    private EntityTemplate() { } // EF Core

    /// <summary>
    /// Creates a new entity template in Draft status.
    /// </summary>
    public static EntityTemplate Create(
        string entityTypeName,
        string displayName,
        Guid gameSystemId,
        Guid ownerId,
        string? description = null,
        Guid? sourceDocumentId = null,
        string? version = null,
        string? iconHint = null)
    {
        if (string.IsNullOrWhiteSpace(entityTypeName))
            throw new ArgumentException("Entity type name cannot be empty", nameof(entityTypeName));
        
        if (string.IsNullOrWhiteSpace(displayName))
            throw new ArgumentException("Display name cannot be empty", nameof(displayName));
        
        // Normalize entity type name (lowercase, no spaces)
        var normalizedTypeName = NormalizeEntityTypeName(entityTypeName);

        return new EntityTemplate
        {
            EntityTypeName = normalizedTypeName,
            DisplayName = displayName.Trim(),
            Description = description?.Trim(),
            GameSystemId = gameSystemId,
            OwnerId = ownerId,
            SourceDocumentId = sourceDocumentId,
            Version = version?.Trim(),
            IconHint = iconHint?.Trim(),
            Status = TemplateStatus.Draft,
            FieldDefinitionsJson = "[]"
        };
    }

    /// <summary>
    /// Gets the field definitions as a list.
    /// </summary>
    public IReadOnlyList<FieldDefinition> GetFieldDefinitions()
    {
        if (string.IsNullOrEmpty(FieldDefinitionsJson) || FieldDefinitionsJson == "[]")
            return Array.Empty<FieldDefinition>();

        var dtos = JsonSerializer.Deserialize<List<FieldDefinitionDto>>(FieldDefinitionsJson);
        if (dtos == null) return Array.Empty<FieldDefinition>();

        return dtos.Select(dto => FieldDefinition.Create(
            dto.Name,
            dto.DisplayName,
            dto.FieldType,
            dto.IsRequired,
            dto.DefaultValue,
            dto.Description,
            dto.Order,
            dto.Options,
            dto.MinValue,
            dto.MaxValue,
            dto.ValidationPattern
        )).OrderBy(f => f.Order).ToList();
    }

    /// <summary>
    /// Sets the field definitions for this template.
    /// Only allowed in Draft or PendingReview status, unless adminOverride is true.
    /// </summary>
    /// <param name="fields">The new field definitions.</param>
    /// <param name="adminOverride">If true, allows updating even if template is Confirmed.</param>
    public void SetFieldDefinitions(IEnumerable<FieldDefinition> fields, bool adminOverride = false)
    {
        if (!adminOverride && Status == TemplateStatus.Confirmed)
            throw new InvalidOperationException("Cannot modify field definitions of a confirmed template");
        
        if (Status == TemplateStatus.Rejected)
            throw new InvalidOperationException("Cannot modify field definitions of a rejected template");

        var fieldList = fields.ToList();
        
        // Validate unique field names
        var duplicates = fieldList
            .GroupBy(f => f.Name.ToLowerInvariant())
            .Where(g => g.Count() > 1)
            .Select(g => g.Key)
            .ToList();
        
        if (duplicates.Any())
            throw new ArgumentException($"Duplicate field names: {string.Join(", ", duplicates)}");

        var dtos = fieldList.Select(f => new FieldDefinitionDto
        {
            Name = f.Name,
            DisplayName = f.DisplayName,
            FieldType = f.FieldType,
            IsRequired = f.IsRequired,
            DefaultValue = f.DefaultValue,
            Description = f.Description,
            Order = f.Order,
            Options = f.GetOptions().ToList(),
            MinValue = f.MinValue,
            MaxValue = f.MaxValue,
            ValidationPattern = f.ValidationPattern
        }).ToList();

        FieldDefinitionsJson = JsonSerializer.Serialize(dtos);
    }

    /// <summary>
    /// Adds a single field definition to this template.
    /// </summary>
    public void AddField(FieldDefinition field)
    {
        var current = GetFieldDefinitions().ToList();
        
        if (current.Any(f => f.Name.Equals(field.Name, StringComparison.OrdinalIgnoreCase)))
            throw new ArgumentException($"Field with name '{field.Name}' already exists");
        
        current.Add(field);
        SetFieldDefinitions(current);
    }

    /// <summary>
    /// Removes a field definition by name.
    /// </summary>
    public void RemoveField(string fieldName)
    {
        var current = GetFieldDefinitions().ToList();
        var field = current.FirstOrDefault(f => f.Name.Equals(fieldName, StringComparison.OrdinalIgnoreCase));
        
        if (field == null)
            throw new ArgumentException($"Field with name '{fieldName}' not found");
        
        current.Remove(field);
        SetFieldDefinitions(current);
    }

    /// <summary>
    /// Updates the template metadata.
    /// Only allowed in Draft or PendingReview status, unless adminOverride is true.
    /// </summary>
    /// <param name="displayName">New display name.</param>
    /// <param name="description">New description.</param>
    /// <param name="iconHint">New icon hint.</param>
    /// <param name="version">New version.</param>
    /// <param name="adminOverride">If true, allows updating even if template is Confirmed.</param>
    public void Update(
        string displayName,
        string? description = null,
        string? iconHint = null,
        string? version = null,
        bool adminOverride = false)
    {
        if (!adminOverride && Status == TemplateStatus.Confirmed)
            throw new InvalidOperationException("Cannot modify a confirmed template");
        
        if (Status == TemplateStatus.Rejected)
            throw new InvalidOperationException("Cannot modify a rejected template");
        
        if (string.IsNullOrWhiteSpace(displayName))
            throw new ArgumentException("Display name cannot be empty", nameof(displayName));

        DisplayName = displayName.Trim();
        Description = description?.Trim();
        IconHint = iconHint?.Trim();
        Version = version?.Trim();
    }

    /// <summary>
    /// Submits the template for review.
    /// </summary>
    public void SubmitForReview()
    {
        if (Status != TemplateStatus.Draft)
            throw new InvalidOperationException($"Cannot submit template for review from status {Status}");
        
        if (!GetFieldDefinitions().Any())
            throw new InvalidOperationException("Cannot submit template without field definitions");

        Status = TemplateStatus.PendingReview;
    }

    /// <summary>
    /// Confirms the template, making it available for entity creation.
    /// </summary>
    public void Confirm(Guid confirmedByUserId, string? notes = null)
    {
        if (Status != TemplateStatus.PendingReview && Status != TemplateStatus.Draft)
            throw new InvalidOperationException($"Cannot confirm template from status {Status}");
        
        if (!GetFieldDefinitions().Any())
            throw new InvalidOperationException("Cannot confirm template without field definitions");

        Status = TemplateStatus.Confirmed;
        ConfirmedAt = DateTime.UtcNow;
        ConfirmedByUserId = confirmedByUserId;
        ReviewNotes = notes?.Trim();
    }

    /// <summary>
    /// Rejects the template with notes.
    /// </summary>
    public void Reject(string? notes = null)
    {
        if (Status != TemplateStatus.PendingReview && Status != TemplateStatus.Draft)
            throw new InvalidOperationException($"Cannot reject template from status {Status}");

        Status = TemplateStatus.Rejected;
        ReviewNotes = notes?.Trim();
    }

    /// <summary>
    /// Reverts a confirmed or rejected template back to draft for modifications.
    /// </summary>
    public void RevertToDraft()
    {
        Status = TemplateStatus.Draft;
        ConfirmedAt = null;
        ConfirmedByUserId = null;
    }

    /// <summary>
    /// Checks if this template can be used for entity creation.
    /// </summary>
    public bool CanBeUsedForEntityCreation => Status == TemplateStatus.Confirmed;

    /// <summary>
    /// Checks if this template is owned by the specified user.
    /// </summary>
    public bool IsOwnedBy(Guid userId) => OwnerId == userId;

    /// <summary>
    /// Validates that the provided attributes conform to this template's field definitions.
    /// </summary>
    public ValidationResult ValidateEntityAttributes(IDictionary<string, object?> attributes)
    {
        var errors = new List<string>();
        var fields = GetFieldDefinitions();

        // Check required fields
        foreach (var field in fields.Where(f => f.IsRequired))
        {
            if (!attributes.TryGetValue(field.Name, out var value) || value == null)
            {
                errors.Add($"Required field '{field.DisplayName}' is missing");
            }
        }

        // Validate provided fields
        foreach (var (key, value) in attributes)
        {
            var field = fields.FirstOrDefault(f => f.Name.Equals(key, StringComparison.OrdinalIgnoreCase));
            if (field == null)
            {
                // Unknown field - could be a warning but not an error
                continue;
            }

            if (!field.ValidateValue(value))
            {
                errors.Add($"Field '{field.DisplayName}' has an invalid value");
            }
        }

        return new ValidationResult(errors.Count == 0, errors);
    }

    /// <summary>
    /// Normalizes an entity type name to a consistent format.
    /// </summary>
    public static string NormalizeEntityTypeName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Entity type name cannot be empty", nameof(name));
        
        // Convert to lowercase, replace spaces with underscores
        return name.Trim().ToLowerInvariant().Replace(" ", "_").Replace("-", "_");
    }

    /// <summary>
    /// Internal DTO for JSON serialization of field definitions.
    /// </summary>
    private class FieldDefinitionDto
    {
        public string Name { get; set; } = null!;
        public string DisplayName { get; set; } = null!;
        public FieldType FieldType { get; set; }
        public bool IsRequired { get; set; }
        public string? DefaultValue { get; set; }
        public string? Description { get; set; }
        public int Order { get; set; }
        public List<string>? Options { get; set; }
        public decimal? MinValue { get; set; }
        public decimal? MaxValue { get; set; }
        public string? ValidationPattern { get; set; }
    }
}

/// <summary>
/// Result of entity attribute validation against a template.
/// </summary>
public record ValidationResult(bool IsValid, IReadOnlyList<string> Errors);
