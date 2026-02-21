namespace Loremaster.Application.Features.EntityGeneration.DTOs;

public record EncounterGenerationRequest
{
    public Guid? GameSystemId { get; init; }
    public string EncounterType { get; init; } = "combat";
    public string Difficulty { get; init; } = "MEDIUM";
    public string Environment { get; init; } = "open-area";
    public string EnemyCount { get; init; } = "squad";
    public bool GenerateImage { get; init; } = true;
}

public record EncounterGenerationResponse : GenerationResponseBase
{
    public string? EncounterJson { get; init; }
    public string? ImageBase64 { get; init; }
    public string? ImageUrl { get; init; }
}
