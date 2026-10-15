using System.Text.Json;

namespace Loremaster.Application.Features.EntityTemplates.Commands.ExtractTemplatesFromManual.Services;

/// <summary>
/// Converts a nested schema <see cref="JsonElement"/> returned by the LLM
/// (sections such as <c>attributes</c>, <c>skills</c>, <c>derived_stats</c>, …)
/// into a flat list of <see cref="ExtractedField"/> objects.
/// </summary>
internal static class SchemaFlattener
{
    /// <summary>
    /// Iterates the top-level sections of <paramref name="schema"/> and converts each
    /// field entry into an <see cref="ExtractedField"/>.
    /// </summary>
    public static List<ExtractedField> Flatten(JsonElement schema)
    {
        var fields = new List<ExtractedField>();
        var order = 0;

        foreach (var section in schema.EnumerateObject())
        {
            if (section.Value.ValueKind == JsonValueKind.Object)
                fields.AddRange(ProcessSection(section.Name, section.Value, ref order));
        }

        return fields;
    }

    // ── Private helpers ────────────────────────────────────────────────────────

    private static IEnumerable<ExtractedField> ProcessSection(
        string sectionName, JsonElement section, ref int order)
    {
        var fields = new List<ExtractedField>();

        foreach (var item in section.EnumerateObject())
        {
            var field = new ExtractedField
            {
                Name = $"{sectionName}_{item.Name}",
                Order = order++
            };

            switch (item.Value.ValueKind)
            {
                case JsonValueKind.String:
                    field.DisplayName = FormatDisplayName(item.Name);
                    field.FieldType = InferFromString(item.Value.GetString() ?? "string");
                    field.Description = $"{sectionName}: {item.Name}";
                    break;

                case JsonValueKind.Object:
                    PopulateFromObject(sectionName, item.Name, item.Value, field);
                    break;

                case JsonValueKind.Array:
                    field.DisplayName = FormatDisplayName(item.Name);
                    field.FieldType = "Json";
                    field.Description = $"{sectionName}: {item.Name} (list)";
                    break;

                default:
                    field.DisplayName = FormatDisplayName(item.Name);
                    field.FieldType = InferFromJsonKind(item.Value.ValueKind);
                    break;
            }

            fields.Add(field);
        }

        return fields;
    }

    private static void PopulateFromObject(
        string sectionName, string fieldName, JsonElement value, ExtractedField field)
    {
        field.DisplayName = GetString(value, "name") ?? FormatDisplayName(fieldName);
        field.Description = GetString(value, "description");

        if (value.TryGetProperty("range", out var range))
        {
            field.FieldType = "Number";
            field.MinValue = GetDecimal(range, "min");
            field.MaxValue = GetDecimal(range, "max");
        }
        else if (value.TryGetProperty("formula", out _))
        {
            field.FieldType = "Text";
            field.Description = $"{field.Description} (Formula: {GetString(value, "formula")})";
        }
        else if (value.TryGetProperty("attribute", out var attr))
        {
            field.FieldType = "Number";
            field.Description = $"{field.Description} (Based on: {attr.GetString()})";
            field.MinValue = 0;
            field.MaxValue = 5;
        }
        else if (value.TryGetProperty("type", out var typeHint))
        {
            field.FieldType = InferFromString(typeHint.GetString() ?? "text");
        }
        else
        {
            field.FieldType = "Text";
        }
    }

    // ── Type inference ─────────────────────────────────────────────────────────

    private static string InferFromString(string typeStr) =>
        typeStr.ToLowerInvariant() switch
        {
            "string" or "text" => "Text",
            "number" or "int" or "integer" or "float" or "decimal" => "Number",
            "boolean" or "bool" => "Boolean",
            "array" or "list" => "Json",
            "object" or "json" => "Json",
            "date" or "datetime" => "Date",
            _ when typeStr.Contains('|') => "Select",   // enum-like: "a | b | c"
            _ => "Text"
        };

    private static string InferFromJsonKind(JsonValueKind kind) =>
        kind switch
        {
            JsonValueKind.Number => "Number",
            JsonValueKind.True or JsonValueKind.False => "Boolean",
            JsonValueKind.Array or JsonValueKind.Object => "Json",
            _ => "Text"
        };

    // ── JSON helpers ───────────────────────────────────────────────────────────

    private static string? GetString(JsonElement el, string prop) =>
        el.TryGetProperty(prop, out var v) && v.ValueKind == JsonValueKind.String
            ? v.GetString()
            : null;

    private static decimal? GetDecimal(JsonElement el, string prop) =>
        el.TryGetProperty(prop, out var v) && v.ValueKind == JsonValueKind.Number
            ? v.GetDecimal()
            : null;

    // ── Display-name formatting ────────────────────────────────────────────────

    private static string FormatDisplayName(string name)
    {
        if (string.IsNullOrEmpty(name)) return name;
        var words = name.Replace('_', ' ').Split(' ');
        return string.Join(" ", words.Select(w =>
            string.IsNullOrEmpty(w) ? w : char.ToUpper(w[0]) + w[1..].ToLower()));
    }
}
