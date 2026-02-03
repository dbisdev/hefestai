using Loremaster.Application.Common.Interfaces;
using Loremaster.Application.Features.GameSystems.DTOs;
using Loremaster.Domain.Exceptions;
using MediatR;

namespace Loremaster.Application.Features.GameSystems.Queries.GetGameSystemById;

/// <summary>
/// Handler for GetGameSystemByIdQuery. Returns a game system by its ID.
/// </summary>
public class GetGameSystemByIdQueryHandler : IRequestHandler<GetGameSystemByIdQuery, GameSystemDto>
{
    private readonly IGameSystemRepository _gameSystemRepository;

    public GetGameSystemByIdQueryHandler(IGameSystemRepository gameSystemRepository)
    {
        _gameSystemRepository = gameSystemRepository;
    }

    /// <summary>
    /// Handles the get game system by ID query.
    /// </summary>
    /// <param name="request">The query with game system ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The game system as a DTO.</returns>
    /// <exception cref="NotFoundException">Thrown when game system not found.</exception>
    public async Task<GameSystemDto> Handle(
        GetGameSystemByIdQuery request, 
        CancellationToken cancellationToken)
    {
        var gameSystem = await _gameSystemRepository.GetByIdAsync(request.Id, cancellationToken);
        
        if (gameSystem == null)
        {
            throw new NotFoundException("GameSystem", request.Id);
        }

        return GameSystemDto.FromEntity(gameSystem);
    }
}
