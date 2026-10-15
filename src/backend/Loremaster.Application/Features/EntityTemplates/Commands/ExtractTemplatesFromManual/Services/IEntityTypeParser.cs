namespace Loremaster.Application.Features.EntityTemplates.Commands.ExtractTemplatesFromManual.Services;

/// <summary>
/// Parses the raw LLM JSON response from entity-type extraction into a structured list of
/// <see cref="ExtractedEntityType"/> objects and normalises their entity type names to
/// canonical values.
/// </summary>
internal interface IEntityTypeParser
{
    /// <summary>
    /// Parses the raw LLM answer, normalises entity type names, flattens nested schemas into
    /// flat field lists, and deduplicates by entity type name (keeping the version with the
    /// most fields).
    /// </summary>
    /// <param name="rawLlmResponse">Raw JSON string returned by the LLM.</param>
    /// <returns>
    /// Deduplicated list of <see cref="ExtractedEntityType"/>; empty when parsing fails
    /// completely.
    /// </returns>
    IReadOnlyList<ExtractedEntityType> ParseAndNormalize(string rawLlmResponse);
}
