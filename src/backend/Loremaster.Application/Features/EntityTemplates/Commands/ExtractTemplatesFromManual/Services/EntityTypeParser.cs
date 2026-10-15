using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;

namespace Loremaster.Application.Features.EntityTemplates.Commands.ExtractTemplatesFromManual.Services;

/// <summary>
/// Implements <see cref="IEntityTypeParser"/>: parses and normalises the raw LLM response
/// returned by the entity-type extraction RAG call.
/// </summary>
internal sealed partial class EntityTypeParser : IEntityTypeParser
{
    // ── Normalisation table ────────────────────────────────────────────────────

    /// <summary>
    /// Maps entity type names extracted by the LLM (English or Spanish) to canonical names.
    /// Keys are lower-case; lookup is case-insensitive via <see cref="StringComparer.OrdinalIgnoreCase"/>.
    /// </summary>
    private static readonly Dictionary<string, string> NormalizationMap =
        new(StringComparer.OrdinalIgnoreCase)
        {
            // character
            ["player_character"] = "character", ["playercharacter"] = "character",
            ["player"] = "character", ["pc"] = "character", ["hero"] = "character",
            ["personaje"] = "character", ["personaje_jugador"] = "character",
            ["pj"] = "character", ["protagonista"] = "character",

            // actor (NPC)
            ["npc"] = "actor", ["non_player_character"] = "actor",
            ["nonplayercharacter"] = "actor",
            ["enemy"] = "actor", ["enemies"] = "actor",
            ["ally"] = "actor", ["allies"] = "actor",
            ["contact"] = "actor", ["contacts"] = "actor",
            ["antagonist"] = "actor", ["pnj"] = "actor",
            ["personaje_no_jugador"] = "actor",
            ["enemigo"] = "actor", ["enemigos"] = "actor",
            ["aliado"] = "actor", ["aliados"] = "actor",
            ["contacto"] = "actor", ["adversario"] = "actor",

            // vehicle
            ["starship"] = "vehicle", ["spacecraft"] = "vehicle", ["ship"] = "vehicle",
            ["car"] = "vehicle", ["bike"] = "vehicle", ["aircraft"] = "vehicle",
            ["mech"] = "vehicle", ["robot"] = "vehicle",
            ["vehiculo"] = "vehicle", ["vehículo"] = "vehicle",
            ["nave"] = "vehicle", ["nave_espacial"] = "vehicle",
            ["astronave"] = "vehicle", ["coche"] = "vehicle",
            ["motocicleta"] = "vehicle", ["avion"] = "vehicle", ["avión"] = "vehicle",

            // monster
            ["creature"] = "monster", ["beast"] = "monster", ["alien"] = "monster",
            ["monsters"] = "monster", ["creatures"] = "monster",
            ["animals"] = "monster", ["fauna"] = "monster",
            ["monstruo"] = "monster", ["monstruos"] = "monster",
            ["criatura"] = "monster", ["criaturas"] = "monster",
            ["bestia"] = "monster", ["bestias"] = "monster",
            ["extraterrestre"] = "monster",
            ["alienigena"] = "monster", ["alienígena"] = "monster",
        };

    // ── Partial-data fallback regex ───────────────────────────────────────────

    /// <summary>Matches individual entity-type objects when full JSON parsing fails.</summary>
    [GeneratedRegex(
        @"\{[^{}]*""entityTypeName""\s*:\s*""(?<typeName>[^""]+)""[^{}]*""displayName""\s*:\s*""(?<displayName>[^""]+)""[^{}]*\}",
        RegexOptions.IgnoreCase)]
    private static partial Regex PartialEntityObjectRegex();

    private readonly ILogger<EntityTypeParser> _logger;

    /// <summary>Initialises the parser with a logger.</summary>
    public EntityTypeParser(ILogger<EntityTypeParser> logger)
    {
        _logger = logger;
    }

    // ── IEntityTypeParser ──────────────────────────────────────────────────────

    /// <inheritdoc/>
    public IReadOnlyList<ExtractedEntityType> ParseAndNormalize(string rawLlmResponse)
    {
        var parsed = TryParseJson(rawLlmResponse)
                     ?? TryExtractPartial(rawLlmResponse);

        if (parsed.Count == 0)
            return Array.Empty<ExtractedEntityType>();

        // Normalise entity type names and flatten nested schemas.
        foreach (var entity in parsed)
        {
            entity.EntityTypeName = NormalizeEntityTypeName(entity.EntityTypeName);

            if (entity.Schema.HasValue && entity.Fields.Count == 0)
                entity.Fields = SchemaFlattener.Flatten(entity.Schema.Value);
        }

        return Deduplicate(parsed);
    }

    // ── Private helpers ────────────────────────────────────────────────────────

    /// <summary>
    /// Normalises an extracted name to a canonical entity type name.
    /// Returns a sanitized fallback when no mapping is found.
    /// </summary>
    private static string NormalizeEntityTypeName(string extractedName)
    {
        if (string.IsNullOrWhiteSpace(extractedName))
            return "unknown";

        var lower = extractedName.ToLowerInvariant().Trim();

        if (NormalizationMap.TryGetValue(lower, out var canonical))
            return canonical;

        // Prefix/suffix containment — intentionally strict: only exact key match matters here.
        // (B24: the original used Contains which caused false positives; removed that path.)
        foreach (var (key, value) in NormalizationMap)
        {
            if (lower == key)
                return value;
        }

        return FieldNameSanitizer.Sanitize(lower);
    }

    /// <summary>
    /// Attempts to parse the raw string as a JSON array.
    /// Returns <see langword="null"/> on failure.
    /// </summary>
    private List<ExtractedEntityType>? TryParseJson(string raw)
    {
        try
        {
            var jsonStart = raw.IndexOf('[');
            var jsonEnd = raw.LastIndexOf(']');

            if (jsonStart < 0 || jsonEnd <= jsonStart)
            {
                _logger.LogWarning("Could not find JSON array in RAG response");
                return null;
            }

            var jsonSlice = raw.AsSpan(jsonStart, jsonEnd - jsonStart + 1).ToString();
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            return JsonSerializer.Deserialize<List<ExtractedEntityType>>(jsonSlice, options);
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "Failed to parse RAG response as JSON; attempting fallback extraction");
            return null;
        }
    }

    /// <summary>
    /// Fallback: uses a regex to extract shallow entity objects from a malformed response.
    /// </summary>
    private List<ExtractedEntityType> TryExtractPartial(string raw)
    {
        var result = new List<ExtractedEntityType>();
        try
        {
            var matches = PartialEntityObjectRegex().Matches(raw);
            foreach (Match m in matches)
            {
                result.Add(new ExtractedEntityType
                {
                    EntityTypeName = m.Groups["typeName"].Value,
                    DisplayName = m.Groups["displayName"].Value,
                });
            }

            if (result.Count > 0)
                _logger.LogInformation("Extracted {Count} entity types via fallback parsing", result.Count);
            else
                _logger.LogWarning("Fallback parsing also found no entity types");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Fallback parsing threw an exception");
        }
        return result;
    }

    /// <summary>
    /// Deduplicates by <see cref="ExtractedEntityType.EntityTypeName"/> (case-insensitive),
    /// keeping the instance with the most fields.
    /// </summary>
    private static IReadOnlyList<ExtractedEntityType> Deduplicate(
        IEnumerable<ExtractedEntityType> items) =>
        items
            .GroupBy(e => e.EntityTypeName?.ToLowerInvariant() ?? string.Empty)
            .Select(g => g.OrderByDescending(e => e.Fields.Count).First())
            .ToList();
}
