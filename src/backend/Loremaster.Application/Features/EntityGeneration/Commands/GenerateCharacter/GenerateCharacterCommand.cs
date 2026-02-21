using Loremaster.Application.Features.EntityGeneration.Commands.Shared;
using MediatR;

namespace Loremaster.Application.Features.EntityGeneration.Commands.GenerateCharacter;

public record GenerateCharacterCommand : IRequest<CharacterGenerationResult>
{
    public Guid UserId { get; init; }
    public Guid? GameSystemId { get; init; }
    public string Species { get; init; } = "human";
    public string Role { get; init; } = "operative";
    public string Morphology { get; init; } = "NEUTRAL";
    public string Attire { get; init; } = "Techwear";
    public bool GenerateImage { get; init; } = true;
}
