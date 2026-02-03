using Loremaster.Application.Features.Campaigns.DTOs;
using MediatR;

namespace Loremaster.Application.Features.Campaigns.Commands.UpdateCampaignStatus;

/// <summary>
/// Command to activate or deactivate a campaign. Only Masters can change status.
/// An inactive campaign does not accept new members.
/// </summary>
/// <param name="CampaignId">The ID of the campaign.</param>
/// <param name="IsActive">True to activate, false to deactivate.</param>
public record UpdateCampaignStatusCommand(Guid CampaignId, bool IsActive) : IRequest<CampaignDto>;
