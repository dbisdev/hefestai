using Loremaster.Domain.Enums;
using Loremaster.Domain.ValueObjects;

namespace Loremaster.Application.Features.EntityTemplates.Commands.ExtractTemplatesFromManual.Services;

/// <summary>
/// Implements <see cref="IFieldDefinitionConverter"/>.
/// </summary>
internal sealed class FieldDefinitionConverter : IFieldDefinitionConverter
{
    /// <inheritdoc/>
    public IReadOnlyList<FieldDefinition> Convert(IReadOnlyList<ExtractedField> extractedFields)
    {
        var usedNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var result = new List<FieldDefinition>(extractedFields.Count);

        for (var i = 0; i < extractedFields.Count; i++)
        {
            var f = extractedFields[i];
            var fieldType = ParseFieldType(f.FieldType);
            var options = f.Options;

            // Select/MultiSelect without options must fall back to Text.
            if ((fieldType == FieldType.Select || fieldType == FieldType.MultiSelect)
                && (options is null || options.Count == 0))
            {
                fieldType = FieldType.Text;
                options = null;
            }

            // Unique, sanitized identifier.
            var baseName = FieldNameSanitizer.Sanitize(f.Name);
            var uniqueName = MakeUnique(baseName, usedNames);
            usedNames.Add(uniqueName);

            var displayName = string.IsNullOrWhiteSpace(f.DisplayName) ? f.Name : f.DisplayName;

            result.Add(FieldDefinition.Create(
                uniqueName,
                displayName,
                fieldType,
                f.IsRequired,
                f.DefaultValue,
                f.Description,
                f.Order ?? i,
                options,
                f.MinValue,
                f.MaxValue,
                f.ValidationPattern));
        }

        return result;
    }

    // ── Private helpers ────────────────────────────────────────────────────────

    /// <summary>
    /// Returns <paramref name="baseName"/> unchanged if it is not yet in
    /// <paramref name="used"/>, otherwise appends an incrementing numeric suffix.
    /// </summary>
    private static string MakeUnique(string baseName, ICollection<string> used)
    {
        var candidate = baseName;
        var counter = 1;
        while (used.Contains(candidate))
            candidate = $"{baseName}_{counter++}";
        return candidate;
    }

    /// <summary>
    /// Maps a string type hint from the LLM to a <see cref="FieldType"/> enum value.
    /// Defaults to <see cref="FieldType.Text"/> for unknown values.
    /// </summary>
    private static FieldType ParseFieldType(string fieldType) =>
        fieldType.ToLowerInvariant() switch
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
