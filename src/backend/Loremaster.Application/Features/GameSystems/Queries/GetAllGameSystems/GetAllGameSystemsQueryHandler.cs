using Loremaster.Application.Common.Interfaces;
using Loremaster.Application.Features.GameSystems.DTOs;
using MediatR;

namespace Loremaster.Application.Features.GameSystems.Queries.GetAllGameSystems;

/// <summary>
/// Handler for GetAllGameSystemsQuery.
/// Returns active game systems filtered by the caller's role and ownership.
/// </summary>
public class GetAllGameSystemsQueryHandler : IRequestHandler<GetAllGameSystemsQuery, IEnumerable<GameSystemDto>>
{
    private readonly IGameSystemRepository _gameSystemRepository;

    public GetAllGameSystemsQueryHandler(IGameSystemRepository gameSystemRepository)
    {
        _gameSystemRepository = gameSystemRepository;
    }

    /// <summary>
    /// Handles the get-all-game-systems query applying ownership-based visibility rules.
    /// </summary>
    /// <param name="request">The query carrying caller identity and role flags.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Active game systems the caller is allowed to see.</returns>
    public async Task<IEnumerable<GameSystemDto>> Handle(
        GetAllGameSystemsQuery request,
        CancellationToken cancellationToken)
    {
        var gameSystems = await _gameSystemRepository.GetActiveForUserAsync(
            request.UserId,
            request.IncludeAll,
            request.IsPlayer,
            cancellationToken);

        return gameSystems.Select(GameSystemDto.FromEntity);
    }
}
