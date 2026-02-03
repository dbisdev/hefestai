namespace Loremaster.Domain.Enums;

/// <summary>
/// Data type for template field definitions.
/// Determines validation and UI rendering.
/// </summary>
public enum FieldType
{
    /// <summary>
    /// Short text input (single line).
    /// </summary>
    Text = 0,
    
    /// <summary>
    /// Long text input (multiline/textarea).
    /// </summary>
    TextArea = 1,
    
    /// <summary>
    /// Integer or decimal number.
    /// </summary>
    Number = 2,
    
    /// <summary>
    /// Boolean true/false.
    /// </summary>
    Boolean = 3,
    
    /// <summary>
    /// Selection from predefined options.
    /// </summary>
    Select = 4,
    
    /// <summary>
    /// Multiple selections from predefined options.
    /// </summary>
    MultiSelect = 5,
    
    /// <summary>
    /// Date value.
    /// </summary>
    Date = 6,
    
    /// <summary>
    /// URL or image reference.
    /// </summary>
    Url = 7,
    
    /// <summary>
    /// Structured JSON data.
    /// </summary>
    Json = 8
}
