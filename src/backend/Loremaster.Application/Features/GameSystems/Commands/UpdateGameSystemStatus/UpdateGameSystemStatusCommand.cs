using Loremaster.Application.Features.GameSystems.DTOs;
using MediatR;

namespace Loremaster.Application.Features.GameSystems.Commands.UpdateGameSystemStatus;

/// <summary>
/// Command to activate or deactivate a game system. Admin only.
/// </summary>
/// <param name="Id">The game system ID.</param>
/// <param name="IsActive">Whether to activate (true) or deactivate (false).</param>
public record UpdateGameSystemStatusCommand(
    Guid Id,
    bool IsActive
) : IRequest<GameSystemDto>;
