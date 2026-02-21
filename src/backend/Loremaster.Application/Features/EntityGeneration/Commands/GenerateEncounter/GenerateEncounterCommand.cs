using Loremaster.Application.Features.EntityGeneration.Commands.Shared;
using MediatR;

namespace Loremaster.Application.Features.EntityGeneration.Commands.GenerateEncounter;

public record GenerateEncounterCommand : IRequest<EncounterGenerationResult>
{
    public Guid UserId { get; init; }
    public Guid? GameSystemId { get; init; }
    public string EncounterType { get; init; } = "combat";
    public string Difficulty { get; init; } = "MEDIUM";
    public string Environment { get; init; } = "open-area";
    public string EnemyCount { get; init; } = "squad";
    public bool GenerateImage { get; init; } = true;
}
