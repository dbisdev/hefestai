using System.Text.Json;
using System.Text.RegularExpressions;

namespace Loremaster.Shared.Helpers;

public static class JsonSanitizationHelper
{
    private static readonly Regex CodeFencePattern = new(
        @"^```(?:\w+)?\s*\n?|\n?```\s*$",
        RegexOptions.Compiled);

    private static readonly HashSet<string> AllowedJsonTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "string",
        "number",
        "boolean",
        "object",
        "array",
        "null"
    };

    /// <summary>
    /// Strips markdown code fences from a JSON string.
    /// Handles ```json, ```xml, ```, and other code fence variations.
    /// </summary>
    /// <param name="json">The JSON string potentially containing markdown code fences</param>
    /// <returns>The cleaned JSON string without code fences</returns>
    public static string StripMarkdownCodeFences(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return string.Empty;

        var cleaned = json.Trim();
        
        if (!cleaned.StartsWith("```"))
            return cleaned;

        // Remove opening and closing code fences
        cleaned = CodeFencePattern.Replace(cleaned, "");
        
        return cleaned.Trim();
    }

    private static readonly Regex DangerousPatternRegex = new(
        @"("+
        @"\$type\s*:" +
        @"|__type\s*:" +
        @"|__proto__" +
        @"|constructor\s*:" +
        @"|prototype\s*:" +
        @"|java\.lang\.Runtime" +
        @"|java\.security\.ObjectInputStream" +
        @"|System\.Diagnostics\.Process" +
        @"|cmd\.exe" +
        @"|powershell" +
        @"|bash" +
        @"|sh\s+-i" +
        @"|curl\s+" +
        @"|wget\s+" +
        @"|nc\s+" +
        @"|netcat",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    public static (bool IsValid, string? ErrorMessage) ValidateJsonInput(string? jsonString)
    {
        if (string.IsNullOrWhiteSpace(jsonString))
        {
            return (true, null);
        }

        if (DangerousPatternRegex.IsMatch(jsonString))
        {
            return (false, "Input contains potentially dangerous patterns");
        }

        try
        {
            using var doc = JsonDocument.Parse(jsonString);
            return ValidateElement(doc.RootElement, "");
        }
        catch (JsonException ex)
        {
            return (false, $"Invalid JSON: {ex.Message}");
        }
    }

    public static (bool IsValid, string? ErrorMessage) ValidateDictionaryInput(Dictionary<string, object>? dictionary)
    {
        if (dictionary == null || dictionary.Count == 0)
        {
            return (true, null);
        }

        foreach (var kvp in dictionary)
        {
            if (string.IsNullOrWhiteSpace(kvp.Key))
            {
                return (false, "Dictionary key cannot be empty");
            }

            if (kvp.Key.StartsWith("$") || kvp.Key.StartsWith("__"))
            {
                return (false, $"Dictionary key '{kvp.Key}' is not allowed");
            }

            var (isValid, error) = ValidateValue(kvp.Value, $"key '{kvp.Key}'");
            if (!isValid)
            {
                return (false, error);
            }
        }

        return (true, null);
    }

    private static (bool IsValid, string? ErrorMessage) ValidateElement(JsonElement element, string path)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                foreach (var property in element.EnumerateObject())
                {
                    var propertyPath = string.IsNullOrEmpty(path) ? property.Name : $"{path}.{property.Name}";
                    
                    if (property.Name.StartsWith("$") || property.Name.StartsWith("__"))
                    {
                        return (false, $"Property '{propertyPath}' is not allowed");
                    }

                    var (isValid, error) = ValidateElement(property.Value, propertyPath);
                    if (!isValid)
                    {
                        return (false, error);
                    }
                }
                break;

            case JsonValueKind.Array:
                var index = 0;
                foreach (var item in element.EnumerateArray())
                {
                    var itemPath = $"{path}[{index}]";
                    var (isValid, error) = ValidateElement(item, itemPath);
                    if (!isValid)
                    {
                        return (false, error);
                    }
                    index++;
                }
                break;
        }

        return (true, null);
    }

    private static (bool IsValid, string? ErrorMessage) ValidateValue(object? value, string path)
    {
        if (value == null)
        {
            return (true, null);
        }

        if (value is JsonElement element)
        {
            return ValidateElement(element, path);
        }

        var type = value.GetType().Name;
        
        if (value is string strValue)
        {
            if (DangerousPatternRegex.IsMatch(strValue))
            {
                return (false, $"Value at '{path}' contains dangerous patterns");
            }
            
            if (strValue.Length > 10000)
            {
                return (false, $"String value at '{path}' exceeds maximum length of 10000");
            }
        }

        if (value is IDictionary<object, object> dict)
        {
            foreach (var key in dict.Keys)
            {
                if (key is string keyStr && (keyStr.StartsWith("$") || keyStr.StartsWith("__")))
                {
                    return (false, $"Dictionary key '{keyStr}' at '{path}' is not allowed");
                }
            }
        }

        return (true, null);
    }

    public static Dictionary<string, object> SanitizeDictionary(Dictionary<string, object>? input)
    {
        if (input == null)
        {
            return new Dictionary<string, object>();
        }

        var sanitized = new Dictionary<string, object>();

        foreach (var kvp in input)
        {
            var sanitizedKey = SanitizeKey(kvp.Key);
            sanitized[sanitizedKey] = SanitizeValue(kvp.Value);
        }

        return sanitized;
    }

    private static string SanitizeKey(string key)
    {
        var sanitized = key.Replace("$", "_").Replace("__", "_");
        return sanitized;
    }

    private static object SanitizeValue(object? value)
    {
        if (value == null)
        {
            return JsonValueKind.Null.ToString();
        }

        if (value is JsonElement element)
        {
            return element.ToString();
        }

        if (value is IDictionary<object, object> dict)
        {
            var sanitized = new Dictionary<string, object>();
            foreach (var kvp in dict)
            {
                var keyStr = kvp.Key?.ToString() ?? "";
                sanitized[SanitizeKey(keyStr)] = SanitizeValue(kvp.Value);
            }
            return sanitized;
        }

        if (value is IEnumerable<object> array)
        {
            return array.Select(SanitizeValue).ToList();
        }

        return value;
    }
}
