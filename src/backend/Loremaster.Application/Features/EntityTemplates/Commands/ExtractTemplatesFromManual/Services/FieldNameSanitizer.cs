namespace Loremaster.Application.Features.EntityTemplates.Commands.ExtractTemplatesFromManual.Services;

/// <summary>
/// Converts a raw field name from the LLM response into a valid snake_case identifier.
/// </summary>
internal static class FieldNameSanitizer
{
    /// <summary>
    /// Sanitizes <paramref name="name"/> to produce a valid, lowercase snake_case identifier:
    /// <list type="bullet">
    ///   <item>Converts to lowercase and replaces spaces, hyphens and dots with underscores.</item>
    ///   <item>Strips characters that are not letters, digits or underscores.</item>
    ///   <item>Collapses consecutive underscores.</item>
    ///   <item>Prepends <c>f_</c> when the first character is not a letter.</item>
    ///   <item>Returns <c>"field"</c> when the result would be empty.</item>
    /// </list>
    /// </summary>
    public static string Sanitize(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return "field";

        var sanitized = name.Trim()
            .ToLowerInvariant()
            .Replace(' ', '_')
            .Replace('-', '_')
            .Replace('.', '_');

        // Keep only letters, digits and underscores.
        sanitized = new string(sanitized.Where(c => char.IsLetterOrDigit(c) || c == '_').ToArray());

        // Collapse consecutive underscores.
        while (sanitized.Contains("__"))
            sanitized = sanitized.Replace("__", "_");

        sanitized = sanitized.Trim('_');

        if (string.IsNullOrEmpty(sanitized))
            return "field";

        if (!char.IsLetter(sanitized[0]))
            sanitized = "f_" + sanitized;

        return sanitized;
    }
}
