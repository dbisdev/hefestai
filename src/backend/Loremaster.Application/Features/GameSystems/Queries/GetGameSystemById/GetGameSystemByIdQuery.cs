using Loremaster.Application.Features.GameSystems.DTOs;
using MediatR;

namespace Loremaster.Application.Features.GameSystems.Queries.GetGameSystemById;

/// <summary>
/// Query to get a game system by its ID.
/// This is a public endpoint that doesn't require authentication.
/// </summary>
/// <param name="Id">The game system ID.</param>
public record GetGameSystemByIdQuery(Guid Id) : IRequest<GameSystemDto>;
