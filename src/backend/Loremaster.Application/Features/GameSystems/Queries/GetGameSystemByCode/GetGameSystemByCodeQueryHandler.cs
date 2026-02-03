using Loremaster.Application.Common.Interfaces;
using Loremaster.Application.Features.GameSystems.DTOs;
using Loremaster.Domain.Exceptions;
using MediatR;

namespace Loremaster.Application.Features.GameSystems.Queries.GetGameSystemByCode;

/// <summary>
/// Handler for GetGameSystemByCodeQuery. Returns a game system by its code.
/// </summary>
public class GetGameSystemByCodeQueryHandler : IRequestHandler<GetGameSystemByCodeQuery, GameSystemDto>
{
    private readonly IGameSystemRepository _gameSystemRepository;

    public GetGameSystemByCodeQueryHandler(IGameSystemRepository gameSystemRepository)
    {
        _gameSystemRepository = gameSystemRepository;
    }

    /// <summary>
    /// Handles the get game system by code query.
    /// </summary>
    /// <param name="request">The query with game system code.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The game system as a DTO.</returns>
    /// <exception cref="NotFoundException">Thrown when game system not found.</exception>
    public async Task<GameSystemDto> Handle(
        GetGameSystemByCodeQuery request, 
        CancellationToken cancellationToken)
    {
        var gameSystem = await _gameSystemRepository.GetByCodeAsync(request.Code, cancellationToken);
        
        if (gameSystem == null)
        {
            throw new NotFoundException("GameSystem", request.Code);
        }

        return GameSystemDto.FromEntity(gameSystem);
    }
}
