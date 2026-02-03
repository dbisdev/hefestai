using Loremaster.Application.Features.GameSystems.DTOs;
using MediatR;

namespace Loremaster.Application.Features.GameSystems.Queries.GetGameSystemByCode;

/// <summary>
/// Query to get a game system by its code.
/// This is a public endpoint that doesn't require authentication.
/// </summary>
/// <param name="Code">The game system code (e.g., "dnd5e", "pathfinder2e").</param>
public record GetGameSystemByCodeQuery(string Code) : IRequest<GameSystemDto>;
