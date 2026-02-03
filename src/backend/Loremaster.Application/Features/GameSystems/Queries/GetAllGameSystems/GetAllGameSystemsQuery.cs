using Loremaster.Application.Features.GameSystems.DTOs;
using MediatR;

namespace Loremaster.Application.Features.GameSystems.Queries.GetAllGameSystems;

/// <summary>
/// Query to get all active game systems.
/// This is a public endpoint that doesn't require authentication.
/// </summary>
public record GetAllGameSystemsQuery() : IRequest<IEnumerable<GameSystemDto>>;
