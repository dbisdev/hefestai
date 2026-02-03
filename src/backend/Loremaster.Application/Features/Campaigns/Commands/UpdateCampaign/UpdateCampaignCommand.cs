using Loremaster.Application.Features.Campaigns.DTOs;
using MediatR;

namespace Loremaster.Application.Features.Campaigns.Commands.UpdateCampaign;

/// <summary>
/// Command to update an existing campaign. Only Masters can update.
/// </summary>
/// <param name="CampaignId">The ID of the campaign to update.</param>
/// <param name="Name">The new campaign name.</param>
/// <param name="Description">Optional new description.</param>
/// <param name="Settings">Optional new game-system-specific settings.</param>
public record UpdateCampaignCommand(
    Guid CampaignId,
    string Name,
    string? Description = null,
    Dictionary<string, object>? Settings = null
) : IRequest<CampaignDetailDto>;
