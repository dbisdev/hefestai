using Loremaster.Application.Features.EntityGeneration.Commands.Shared;
using MediatR;

namespace Loremaster.Application.Features.EntityGeneration.Commands.GenerateSolarSystem;

public record GenerateSolarSystemCommand : IRequest<SolarSystemGenerationResult>
{
    public Guid UserId { get; init; }
    public Guid? GameSystemId { get; init; }
    public string SpectralClass { get; init; } = "G";
    public int PlanetCount { get; init; } = 8;
    public bool GenerateImage { get; init; } = true;
}
