namespace Loremaster.Application.Features.EntityGeneration.DTOs;

public record CharacterGenerationRequest
{
    public Guid? GameSystemId { get; init; }
    public string Species { get; init; } = "human";
    public string Role { get; init; } = "operative";
    public string Morphology { get; init; } = "NEUTRAL";
    public string Attire { get; init; } = "Techwear";
    public bool GenerateImage { get; init; } = true;
}

public record CharacterGenerationResponse : GenerationResponseBase
{
    public string? CharacterJson { get; init; }
    public string? ImageBase64 { get; init; }
    public string? ImageUrl { get; init; }
}
