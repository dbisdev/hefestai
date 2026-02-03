using System.Text.Json;
using Loremaster.Domain.Enums;

namespace Loremaster.Domain.ValueObjects;

/// <summary>
/// Value object representing a field definition within an entity template.
/// Defines the schema for a single field including type, validation rules, and display properties.
/// Immutable by design - modifications create new instances.
/// </summary>
public sealed class FieldDefinition : IEquatable<FieldDefinition>
{
    /// <summary>
    /// Unique identifier for this field within the template.
    /// Used as the key in entity attributes JSON.
    /// </summary>
    public string Name { get; }
    
    /// <summary>
    /// Human-readable label for display in UI.
    /// </summary>
    public string DisplayName { get; }
    
    /// <summary>
    /// Data type of the field.
    /// </summary>
    public FieldType FieldType { get; }
    
    /// <summary>
    /// Whether this field must be provided when creating an entity.
    /// </summary>
    public bool IsRequired { get; }
    
    /// <summary>
    /// Default value for the field (JSON-serialized).
    /// </summary>
    public string? DefaultValue { get; }
    
    /// <summary>
    /// Description or help text for the field.
    /// </summary>
    public string? Description { get; }
    
    /// <summary>
    /// Display order within the template (lower = first).
    /// </summary>
    public int Order { get; }
    
    /// <summary>
    /// Available options for Select/MultiSelect fields (JSON array of strings).
    /// </summary>
    public string? Options { get; }
    
    /// <summary>
    /// Minimum value for Number fields, or minimum length for Text fields.
    /// </summary>
    public decimal? MinValue { get; }
    
    /// <summary>
    /// Maximum value for Number fields, or maximum length for Text fields.
    /// </summary>
    public decimal? MaxValue { get; }
    
    /// <summary>
    /// Regex pattern for validation (Text fields only).
    /// </summary>
    public string? ValidationPattern { get; }

    private FieldDefinition(
        string name,
        string displayName,
        FieldType fieldType,
        bool isRequired,
        string? defaultValue,
        string? description,
        int order,
        string? options,
        decimal? minValue,
        decimal? maxValue,
        string? validationPattern)
    {
        Name = name;
        DisplayName = displayName;
        FieldType = fieldType;
        IsRequired = isRequired;
        DefaultValue = defaultValue;
        Description = description;
        Order = order;
        Options = options;
        MinValue = minValue;
        MaxValue = maxValue;
        ValidationPattern = validationPattern;
    }

    /// <summary>
    /// Creates a new field definition with validation.
    /// </summary>
    public static FieldDefinition Create(
        string name,
        string displayName,
        FieldType fieldType,
        bool isRequired = false,
        string? defaultValue = null,
        string? description = null,
        int order = 0,
        IEnumerable<string>? options = null,
        decimal? minValue = null,
        decimal? maxValue = null,
        string? validationPattern = null)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Field name cannot be empty", nameof(name));
        
        if (string.IsNullOrWhiteSpace(displayName))
            throw new ArgumentException("Display name cannot be empty", nameof(displayName));
        
        // Validate name format (alphanumeric + underscore, starts with letter)
        if (!IsValidFieldName(name))
            throw new ArgumentException(
                "Field name must start with a letter and contain only letters, numbers, and underscores", 
                nameof(name));
        
        // Validate options for Select/MultiSelect fields
        string? optionsJson = null;
        if (fieldType == FieldType.Select || fieldType == FieldType.MultiSelect)
        {
            if (options == null || !options.Any())
                throw new ArgumentException(
                    $"Options are required for {fieldType} fields", 
                    nameof(options));
            optionsJson = JsonSerializer.Serialize(options.ToList());
        }
        
        // Validate min/max for Number fields
        if (minValue.HasValue && maxValue.HasValue && minValue > maxValue)
            throw new ArgumentException("MinValue cannot be greater than MaxValue");

        return new FieldDefinition(
            name.Trim(),
            displayName.Trim(),
            fieldType,
            isRequired,
            defaultValue?.Trim(),
            description?.Trim(),
            order,
            optionsJson,
            minValue,
            maxValue,
            validationPattern?.Trim());
    }

    /// <summary>
    /// Creates a simple text field.
    /// </summary>
    public static FieldDefinition Text(
        string name, 
        string displayName, 
        bool isRequired = false, 
        string? description = null,
        int order = 0,
        int? maxLength = null)
    {
        return Create(
            name, 
            displayName, 
            FieldType.Text, 
            isRequired, 
            description: description,
            order: order,
            maxValue: maxLength);
    }

    /// <summary>
    /// Creates a textarea field for long text.
    /// </summary>
    public static FieldDefinition TextArea(
        string name, 
        string displayName, 
        bool isRequired = false, 
        string? description = null,
        int order = 0)
    {
        return Create(name, displayName, FieldType.TextArea, isRequired, description: description, order: order);
    }

    /// <summary>
    /// Creates a numeric field.
    /// </summary>
    public static FieldDefinition Number(
        string name, 
        string displayName, 
        bool isRequired = false,
        decimal? minValue = null,
        decimal? maxValue = null,
        string? description = null,
        int order = 0)
    {
        return Create(
            name, 
            displayName, 
            FieldType.Number, 
            isRequired, 
            description: description,
            order: order,
            minValue: minValue,
            maxValue: maxValue);
    }

    /// <summary>
    /// Creates a boolean field.
    /// </summary>
    public static FieldDefinition Boolean(
        string name, 
        string displayName, 
        bool defaultValue = false,
        string? description = null,
        int order = 0)
    {
        return Create(
            name, 
            displayName, 
            FieldType.Boolean, 
            isRequired: false, 
            defaultValue: defaultValue.ToString().ToLowerInvariant(),
            description: description,
            order: order);
    }

    /// <summary>
    /// Creates a select dropdown field.
    /// </summary>
    public static FieldDefinition Select(
        string name, 
        string displayName, 
        IEnumerable<string> options,
        bool isRequired = false,
        string? description = null,
        int order = 0)
    {
        return Create(
            name, 
            displayName, 
            FieldType.Select, 
            isRequired, 
            description: description,
            order: order,
            options: options);
    }

    /// <summary>
    /// Gets the options as a list (for Select/MultiSelect fields).
    /// </summary>
    public IReadOnlyList<string> GetOptions()
    {
        if (string.IsNullOrEmpty(Options))
            return Array.Empty<string>();
        
        return JsonSerializer.Deserialize<List<string>>(Options) ?? new List<string>();
    }

    /// <summary>
    /// Validates a value against this field definition.
    /// </summary>
    public bool ValidateValue(object? value)
    {
        // Required check
        if (IsRequired && value == null)
            return false;
        
        if (value == null)
            return true;

        return FieldType switch
        {
            FieldType.Text or FieldType.TextArea => ValidateTextValue(value),
            FieldType.Number => ValidateNumberValue(value),
            FieldType.Boolean => ValidateBooleanValue(value),
            FieldType.Select => ValidateSelectValue(value),
            FieldType.MultiSelect => ValidateMultiSelectValue(value),
            _ => true
        };
    }

    private bool ValidateTextValue(object value)
    {
        var text = value.ToString();
        if (text == null) return false;
        
        if (MinValue.HasValue && text.Length < (int)MinValue.Value)
            return false;
        if (MaxValue.HasValue && text.Length > (int)MaxValue.Value)
            return false;
        if (!string.IsNullOrEmpty(ValidationPattern))
        {
            var regex = new System.Text.RegularExpressions.Regex(ValidationPattern);
            if (!regex.IsMatch(text))
                return false;
        }
        return true;
    }

    private bool ValidateNumberValue(object value)
    {
        if (!decimal.TryParse(value.ToString(), out var number))
            return false;
        
        if (MinValue.HasValue && number < MinValue.Value)
            return false;
        if (MaxValue.HasValue && number > MaxValue.Value)
            return false;
        
        return true;
    }

    private static bool ValidateBooleanValue(object value)
    {
        if (value is bool)
            return true;
        if (value is string s)
            return bool.TryParse(s, out _);
        return false;
    }

    private bool ValidateSelectValue(object value)
    {
        var options = GetOptions();
        return options.Contains(value.ToString());
    }

    private bool ValidateMultiSelectValue(object value)
    {
        var options = GetOptions();
        if (value is IEnumerable<string> values)
            return values.All(v => options.Contains(v));
        return false;
    }

    private static bool IsValidFieldName(string name)
    {
        if (string.IsNullOrEmpty(name)) return false;
        if (!char.IsLetter(name[0])) return false;
        return name.All(c => char.IsLetterOrDigit(c) || c == '_');
    }

    public bool Equals(FieldDefinition? other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;
        return Name == other.Name && 
               DisplayName == other.DisplayName && 
               FieldType == other.FieldType &&
               IsRequired == other.IsRequired;
    }

    public override bool Equals(object? obj) => Equals(obj as FieldDefinition);
    
    public override int GetHashCode() => HashCode.Combine(Name, DisplayName, FieldType, IsRequired);

    public override string ToString() => $"{DisplayName} ({Name}: {FieldType})";
}
