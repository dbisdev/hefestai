using Loremaster.Application.Features.EntityGeneration.Commands.Shared;
using MediatR;

namespace Loremaster.Application.Features.EntityGeneration.Commands.GenerateMission;

public record GenerateMissionCommand : IRequest<MissionGenerationResult>
{
    public Guid UserId { get; init; }
    public Guid? GameSystemId { get; init; }
    public string MissionType { get; init; } = "extraction";
    public string Difficulty { get; init; } = "MEDIUM";
    public string Environment { get; init; } = "space-station";
    public string FactionInvolved { get; init; } = "corporate";
    public bool GenerateImage { get; init; } = true;
}
