using Loremaster.Application.Features.GameSystems.DTOs;
using MediatR;

namespace Loremaster.Application.Features.GameSystems.Commands.CreateGameSystem;

/// <summary>
/// Command to create a new game system. Admin only.
/// </summary>
/// <param name="Code">Unique identifier code (e.g., "dnd5e").</param>
/// <param name="Name">Display name.</param>
/// <param name="Publisher">Publisher name (optional).</param>
/// <param name="Version">Version string (optional).</param>
/// <param name="Description">Description (optional).</param>
/// <param name="SupportedEntityTypes">List of supported entity types (optional).</param>
public record CreateGameSystemCommand(
    string Code,
    string Name,
    string? Publisher = null,
    string? Version = null,
    string? Description = null,
    List<string>? SupportedEntityTypes = null
) : IRequest<GameSystemDto>;
