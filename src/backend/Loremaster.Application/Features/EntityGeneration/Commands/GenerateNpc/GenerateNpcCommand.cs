using Loremaster.Application.Features.EntityGeneration.Commands.Shared;
using MediatR;

namespace Loremaster.Application.Features.EntityGeneration.Commands.GenerateNpc;

public record GenerateNpcCommand : IRequest<NpcGenerationResult>
{
    public Guid UserId { get; init; }
    public Guid? GameSystemId { get; init; }
    public string Species { get; init; } = "human";
    public string Occupation { get; init; } = "merchant";
    public string Personality { get; init; } = "friendly";
    public string Setting { get; init; } = "space-station";
    public bool GenerateImage { get; init; } = true;
}
