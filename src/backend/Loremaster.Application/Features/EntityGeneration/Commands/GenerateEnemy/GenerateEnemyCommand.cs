using Loremaster.Application.Features.EntityGeneration.Commands.Shared;
using MediatR;

namespace Loremaster.Application.Features.EntityGeneration.Commands.GenerateEnemy;

public record GenerateEnemyCommand : IRequest<EnemyGenerationResult>
{
    public Guid UserId { get; init; }
    public Guid? GameSystemId { get; init; }
    public string Species { get; init; } = "alien-beast";
    public string ThreatLevel { get; init; } = "moderate";
    public string Behavior { get; init; } = "aggressive";
    public string Environment { get; init; } = "space-station";
    public bool GenerateImage { get; init; } = true;
}
