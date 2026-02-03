using Loremaster.Application.Common.Interfaces;
using Loremaster.Application.Features.GameSystems.DTOs;
using MediatR;

namespace Loremaster.Application.Features.GameSystems.Queries.GetAllGameSystems;

/// <summary>
/// Handler for GetAllGameSystemsQuery. Returns all active game systems.
/// </summary>
public class GetAllGameSystemsQueryHandler : IRequestHandler<GetAllGameSystemsQuery, IEnumerable<GameSystemDto>>
{
    private readonly IGameSystemRepository _gameSystemRepository;

    public GetAllGameSystemsQueryHandler(IGameSystemRepository gameSystemRepository)
    {
        _gameSystemRepository = gameSystemRepository;
    }

    /// <summary>
    /// Handles the get all game systems query.
    /// </summary>
    /// <param name="request">The query.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Collection of active game systems as DTOs.</returns>
    public async Task<IEnumerable<GameSystemDto>> Handle(
        GetAllGameSystemsQuery request, 
        CancellationToken cancellationToken)
    {
        var gameSystems = await _gameSystemRepository.GetActiveAsync(cancellationToken);
        return gameSystems.Select(GameSystemDto.FromEntity);
    }
}
