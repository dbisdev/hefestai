using Loremaster.Application.Features.GameSystems.DTOs;
using MediatR;

namespace Loremaster.Application.Features.GameSystems.Commands.UpdateGameSystem;

/// <summary>
/// Command to update an existing game system. Admin only.
/// </summary>
/// <param name="Id">The game system ID.</param>
/// <param name="Name">Updated display name.</param>
/// <param name="Publisher">Updated publisher (optional).</param>
/// <param name="Version">Updated version (optional).</param>
/// <param name="Description">Updated description (optional).</param>
/// <param name="SupportedEntityTypes">Updated supported entity types (optional).</param>
public record UpdateGameSystemCommand(
    Guid Id,
    string Name,
    string? Publisher = null,
    string? Version = null,
    string? Description = null,
    List<string>? SupportedEntityTypes = null
) : IRequest<GameSystemDto>;
