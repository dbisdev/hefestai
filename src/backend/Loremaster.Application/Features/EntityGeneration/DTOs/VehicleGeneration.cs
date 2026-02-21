namespace Loremaster.Application.Features.EntityGeneration.DTOs;

public record VehicleGenerationRequest
{
    public Guid? GameSystemId { get; init; }
    public string Type { get; init; } = "starship";
    public string Class { get; init; } = "interceptor";
    public string Engine { get; init; } = "fusion";
    public bool GenerateImage { get; init; } = true;
}

public record VehicleGenerationResponse : GenerationResponseBase
{
    public string? VehicleJson { get; init; }
    public string? ImageBase64 { get; init; }
    public string? ImageUrl { get; init; }
}
