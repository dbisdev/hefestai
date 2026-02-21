namespace Loremaster.Application.Features.EntityGeneration.DTOs;

public record MissionGenerationRequest
{
    public Guid? GameSystemId { get; init; }
    public string MissionType { get; init; } = "extraction";
    public string Difficulty { get; init; } = "MEDIUM";
    public string Environment { get; init; } = "space-station";
    public string FactionInvolved { get; init; } = "corporate";
    public bool GenerateImage { get; init; } = true;
}

public record MissionGenerationResponse : GenerationResponseBase
{
    public string? MissionJson { get; init; }
    public string? ImageBase64 { get; init; }
    public string? ImageUrl { get; init; }
}
