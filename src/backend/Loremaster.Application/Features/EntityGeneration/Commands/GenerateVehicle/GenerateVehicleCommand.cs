using Loremaster.Application.Features.EntityGeneration.Commands.Shared;
using MediatR;

namespace Loremaster.Application.Features.EntityGeneration.Commands.GenerateVehicle;

public record GenerateVehicleCommand : IRequest<VehicleGenerationResult>
{
    public Guid UserId { get; init; }
    public Guid? GameSystemId { get; init; }
    public string Type { get; init; } = "starship";
    public string Class { get; init; } = "interceptor";
    public string Engine { get; init; } = "fusion";
    public bool GenerateImage { get; init; } = true;
}
