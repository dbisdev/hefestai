namespace Loremaster.Application.Features.EntityGeneration.DTOs;

public record EnemyGenerationRequest
{
    public Guid? GameSystemId { get; init; }
    public string Species { get; init; } = "alien-beast";
    public string ThreatLevel { get; init; } = "moderate";
    public string Behavior { get; init; } = "aggressive";
    public string Environment { get; init; } = "space-station";
    public bool GenerateImage { get; init; } = true;
}

public record EnemyGenerationResponse : GenerationResponseBase
{
    public string? EnemyJson { get; init; }
    public string? ImageBase64 { get; init; }
    public string? ImageUrl { get; init; }
}
