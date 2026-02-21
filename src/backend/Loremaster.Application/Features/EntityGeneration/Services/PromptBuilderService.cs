using System.Text.Json;
using Loremaster.Domain.Entities;
using Loremaster.Domain.Enums;
using Loremaster.Domain.ValueObjects;

namespace Loremaster.Application.Features.EntityGeneration.Services;

public class PromptBuilderService : IPromptBuilderService
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    public string BuildExampleJsonFromTemplate(IReadOnlyList<FieldDefinition> fields)
    {
        var orderedFields = fields.OrderBy(f => f.Order).ToList();
        var exampleObject = new Dictionary<string, object>();

        foreach (var field in orderedFields)
        {
            var exampleValue = GenerateExampleValue(field);
            exampleObject[field.Name] = exampleValue;
        }

        return JsonSerializer.Serialize(exampleObject, JsonOptions);
    }

    public string BuildFieldDescriptions(IReadOnlyList<FieldDefinition> fields)
    {
        var orderedFields = fields.OrderBy(f => f.Order).ToList();
        var descriptions = new List<string>();

        foreach (var field in orderedFields)
        {
            var desc = $"- {field.Name}: {GetFieldTypeDescription(field)}";

            if (!string.IsNullOrEmpty(field.Description))
                desc += $" ({field.Description})";

            if (field.IsRequired)
                desc += " [REQUIRED]";

            descriptions.Add(desc);
        }

        return string.Join("\n", descriptions);
    }

    public string BuildFullPromptTrace(string systemPrompt, string userQuery, IEnumerable<string>? ragContext = null)
    {
        var ragContextText = ragContext != null
            ? $"\n\n[RAG CONTEXT]\n{string.Join("\n---\n", ragContext)}"
            : string.Empty;

        return $"[SYSTEM]\n{systemPrompt}\n\n[USER]\n{userQuery}{ragContextText}";
    }

    public (string ExampleJson, string FieldDescriptions) BuildTemplateSchema(
        EntityTemplate template,
        string entityTypeDisplayName)
    {
        var fields = template.GetFieldDefinitions();
        var statsJson = BuildExampleJsonFromTemplate(fields);

        var exampleJson = $@"{{""name"":""<{entityTypeDisplayName} Name>"",""description"":""<2-3 paragraph description>"",""stats"":{statsJson}}}";
        var fieldDescriptions = $@"- name: A unique name fitting the setting
- description: A 2-3 paragraph description
- stats: An object containing the following template fields:
{BuildFieldDescriptions(fields)}";

        return (exampleJson, fieldDescriptions);
    }

    private static object GenerateExampleValue(FieldDefinition field)
    {
        return field.FieldType switch
        {
            FieldType.Text => GenerateTextExample(field),
            FieldType.TextArea => GenerateTextAreaExample(field),
            FieldType.Number => GenerateNumberExample(field),
            FieldType.Boolean => false,
            FieldType.Select => GenerateSelectExample(field),
            FieldType.MultiSelect => GenerateMultiSelectExample(field),
            FieldType.Date => DateTime.UtcNow.ToString("yyyy-MM-dd"),
            FieldType.Url => "https://example.com/image.png",
            FieldType.Json => GenerateJsonExample(field),
            _ => $"<{field.DisplayName}>"
        };
    }

    private static string GenerateTextExample(FieldDefinition field)
    {
        if (!string.IsNullOrEmpty(field.Description))
            return $"<{field.Description}>";
        return $"<{field.DisplayName}>";
    }

    private static string GenerateTextAreaExample(FieldDefinition field)
    {
        if (!string.IsNullOrEmpty(field.Description))
            return $"<{field.Description} - 2-3 paragraphs>";
        return $"<{field.DisplayName} - detailed description>";
    }

    private static object GenerateNumberExample(FieldDefinition field)
    {
        if (field.MinValue.HasValue && field.MaxValue.HasValue)
        {
            var mid = (field.MinValue.Value + field.MaxValue.Value) / 2;
            return Math.Round(mid);
        }

        if (field.MinValue.HasValue)
            return field.MinValue.Value;

        if (field.MaxValue.HasValue)
            return field.MaxValue.Value / 2;

        return 50;
    }

    private static string GenerateSelectExample(FieldDefinition field)
    {
        var options = field.GetOptions();
        return options.FirstOrDefault() ?? $"<{field.DisplayName} option>";
    }

    private static object GenerateMultiSelectExample(FieldDefinition field)
    {
        var options = field.GetOptions();
        return options.Take(2).ToList();
    }

    private static object GenerateJsonExample(FieldDefinition field)
    {
        return new Dictionary<string, object>
        {
            ["key"] = "value",
            ["nested"] = new Dictionary<string, object>
            {
                ["property"] = $"<{field.DisplayName} content>"
            }
        };
    }

    private static string GetFieldTypeDescription(FieldDefinition field)
    {
        return field.FieldType switch
        {
            FieldType.Text => "Short text",
            FieldType.TextArea => "Long text/description",
            FieldType.Number when field.MinValue.HasValue && field.MaxValue.HasValue
                => $"Number ({field.MinValue}-{field.MaxValue})",
            FieldType.Number when field.MinValue.HasValue
                => $"Number (min: {field.MinValue})",
            FieldType.Number when field.MaxValue.HasValue
                => $"Number (max: {field.MaxValue})",
            FieldType.Number => "Number",
            FieldType.Boolean => "Boolean (true/false)",
            FieldType.Select => $"One of: [{string.Join(", ", field.GetOptions())}]",
            FieldType.MultiSelect => $"Array of: [{string.Join(", ", field.GetOptions())}]",
            FieldType.Date => "Date (YYYY-MM-DD)",
            FieldType.Url => "URL string",
            FieldType.Json => "Nested JSON object",
            _ => "Value"
        };
    }
}
