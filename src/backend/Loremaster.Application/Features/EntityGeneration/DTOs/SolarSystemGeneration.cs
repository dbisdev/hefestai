namespace Loremaster.Application.Features.EntityGeneration.DTOs;

public record SolarSystemGenerationRequest
{
    public Guid? GameSystemId { get; init; }
    public string SpectralClass { get; init; } = "G";
    public int PlanetCount { get; init; } = 8;
    public bool GenerateImage { get; init; } = true;
}

public record SolarSystemGenerationResponse : GenerationResponseBase
{
    public string? SystemJson { get; init; }
    public string? ImageBase64 { get; init; }
    public string? ImageUrl { get; init; }
}
