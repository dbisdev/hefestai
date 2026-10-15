using System.Text.Json;

namespace Loremaster.Application.Features.EntityTemplates.Commands.ExtractTemplatesFromManual.Services;

/// <summary>
/// Internal DTO representing a single entity type extracted from a game manual via RAG.
/// Supports both a flat <c>fields</c> array and a nested <c>schema</c> object returned by the LLM.
/// </summary>
internal class ExtractedEntityType
{
    /// <summary>Canonical entity type name (e.g. "character", "actor", "vehicle").</summary>
    public string EntityTypeName { get; set; } = string.Empty;

    /// <summary>Human-readable display name (may be in the manual's language).</summary>
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>Optional description of the entity type.</summary>
    public string? Description { get; set; }

    /// <summary>Optional icon hint for the UI.</summary>
    public string? IconHint { get; set; }

    /// <summary>Flat list of fields extracted directly by the LLM.</summary>
    public List<ExtractedField> Fields { get; set; } = new();

    /// <summary>
    /// Nested schema object returned by the LLM when it groups fields into sections
    /// (attributes, skills, derived_stats, identity, gear, …).
    /// Mutually exclusive with <see cref="Fields"/>: the parser flattens this into
    /// <see cref="Fields"/> when present.
    /// </summary>
    public JsonElement? Schema { get; set; }
}

/// <summary>
/// Internal DTO for a single field within an <see cref="ExtractedEntityType"/>.
/// </summary>
internal class ExtractedField
{
    /// <summary>Snake_case field identifier.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Human-readable label.</summary>
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>Field type hint from the LLM (e.g. "text", "number", "select").</summary>
    public string FieldType { get; set; } = "Text";

    /// <summary>Whether the field is required for entity creation.</summary>
    public bool IsRequired { get; set; }

    /// <summary>Optional default value.</summary>
    public string? DefaultValue { get; set; }

    /// <summary>Optional description or tooltip text.</summary>
    public string? Description { get; set; }

    /// <summary>Display order within the entity type form.</summary>
    public int? Order { get; set; }

    /// <summary>For <c>Select</c> / <c>MultiSelect</c> fields: the list of allowed values.</summary>
    public List<string>? Options { get; set; }

    /// <summary>For <c>Number</c> fields: minimum allowed value.</summary>
    public decimal? MinValue { get; set; }

    /// <summary>For <c>Number</c> fields: maximum allowed value.</summary>
    public decimal? MaxValue { get; set; }

    /// <summary>Optional regex validation pattern.</summary>
    public string? ValidationPattern { get; set; }
}
