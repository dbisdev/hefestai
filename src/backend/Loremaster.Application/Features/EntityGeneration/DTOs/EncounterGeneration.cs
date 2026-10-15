namespace Loremaster.Application.Features.EntityGeneration.DTOs;

/// <summary>
/// Represents the number of enemies in an encounter, expressed as a named size category.
/// Serializes as a lowercase string (e.g. "squad") via the global JsonStringEnumConverter.
/// </summary>
public enum EnemyCountSize
{
    Solo,
    Pair,
    Squad,
    Horde
}

public record EncounterGenerationRequest
{
    public Guid? GameSystemId { get; init; }
    public string EncounterType { get; init; } = "combat";
    public string Difficulty { get; init; } = "MEDIUM";
    public string Environment { get; init; } = "open-area";
    /// <summary>Typed enemy count category — serialized as string (e.g. "Squad").</summary>
    public EnemyCountSize EnemyCount { get; init; } = EnemyCountSize.Squad;
    public bool GenerateImage { get; init; } = true;
}

public record EncounterGenerationResponse : GenerationResponseBase
{
    public string? EncounterJson { get; init; }
    public string? ImageBase64 { get; init; }
    public string? ImageUrl { get; init; }
}
