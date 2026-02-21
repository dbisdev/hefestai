namespace Loremaster.Application.Features.EntityGeneration.DTOs;

public record NpcGenerationRequest
{
    public Guid? GameSystemId { get; init; }
    public string Species { get; init; } = "human";
    public string Occupation { get; init; } = "merchant";
    public string Personality { get; init; } = "friendly";
    public string Setting { get; init; } = "space-station";
    public bool GenerateImage { get; init; } = true;
}

public record NpcGenerationResponse : GenerationResponseBase
{
    public string? NpcJson { get; init; }
    public string? ImageBase64 { get; init; }
    public string? ImageUrl { get; init; }
}
