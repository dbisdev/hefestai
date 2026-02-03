using Loremaster.Domain.Entities;

namespace Loremaster.Application.Features.GameSystems.DTOs;

/// <summary>
/// Game system information DTO.
/// </summary>
public record GameSystemDto
{
    public Guid Id { get; init; }
    public string Code { get; init; } = null!;
    public string Name { get; init; } = null!;
    public string? Publisher { get; init; }
    public string? Version { get; init; }
    public string? Description { get; init; }
    public List<string> SupportedEntityTypes { get; init; } = new();
    public bool IsActive { get; init; }

    /// <summary>
    /// Maps a GameSystem entity to a GameSystemDto.
    /// </summary>
    /// <param name="gameSystem">The game system entity.</param>
    /// <returns>A GameSystemDto instance.</returns>
    public static GameSystemDto FromEntity(GameSystem gameSystem)
    {
        return new GameSystemDto
        {
            Id = gameSystem.Id,
            Code = gameSystem.Code,
            Name = gameSystem.Name,
            Publisher = gameSystem.Publisher,
            Version = gameSystem.Version,
            Description = gameSystem.Description,
            SupportedEntityTypes = gameSystem.SupportedEntityTypes,
            IsActive = gameSystem.IsActive
        };
    }
}
