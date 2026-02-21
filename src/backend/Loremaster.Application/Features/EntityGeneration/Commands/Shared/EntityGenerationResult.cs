namespace Loremaster.Application.Features.EntityGeneration.Commands.Shared;

public record EntityGenerationResult
{
    public bool Success { get; init; }
    public string? EntityJson { get; init; }
    public string? ImageBase64 { get; init; }
    public string? ImageUrl { get; init; }
    public string? Error { get; init; }
    public bool RagContextUsed { get; init; }
    public int RagSourceCount { get; init; }
    public Guid? GenerationRequestId { get; init; }
}

public record CharacterGenerationResult : EntityGenerationResult;
public record SolarSystemGenerationResult : EntityGenerationResult;
public record VehicleGenerationResult : EntityGenerationResult;
public record NpcGenerationResult : EntityGenerationResult;
public record EnemyGenerationResult : EntityGenerationResult;
public record MissionGenerationResult : EntityGenerationResult;
public record EncounterGenerationResult : EntityGenerationResult;
