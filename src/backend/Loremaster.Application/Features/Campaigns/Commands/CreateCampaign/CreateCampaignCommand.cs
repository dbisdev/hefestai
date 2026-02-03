using Loremaster.Application.Features.Campaigns.DTOs;
using MediatR;

namespace Loremaster.Application.Features.Campaigns.Commands.CreateCampaign;

/// <summary>
/// Command to create a new campaign. The creator automatically becomes the Master.
/// </summary>
/// <param name="Name">The campaign name.</param>
/// <param name="GameSystemId">The game system (e.g., D&D 5e, Pathfinder).</param>
/// <param name="Description">Optional campaign description.</param>
/// <param name="Settings">Optional game-system-specific settings as a dictionary.</param>
public record CreateCampaignCommand(
    string Name,
    Guid GameSystemId,
    string? Description = null,
    Dictionary<string, object>? Settings = null
) : IRequest<CampaignDetailDto>;
