using Loremaster.Application.Features.GameSystems.DTOs;
using MediatR;

namespace Loremaster.Application.Features.GameSystems.Queries.GetAllGameSystems;

/// <summary>
/// Query to retrieve active game systems visible to the calling user.
/// Admins see all systems; Masters see Admin-owned systems plus their own;
/// Players see Admin-owned systems plus systems owned by Masters in their campaigns.
/// </summary>
/// <param name="UserId">The ID of the calling user. Null for unauthenticated callers.</param>
/// <param name="IncludeAll">True when the caller is an Admin and should see every active system.</param>
/// <param name="IsPlayer">True when the caller is a Player (triggers campaign-based visibility).</param>
public record GetAllGameSystemsQuery(
    Guid? UserId = null,
    bool IncludeAll = false,
    bool IsPlayer = false) : IRequest<IEnumerable<GameSystemDto>>;
