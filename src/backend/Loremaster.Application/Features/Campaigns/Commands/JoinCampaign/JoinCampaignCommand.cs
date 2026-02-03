using Loremaster.Application.Features.Campaigns.DTOs;
using MediatR;

namespace Loremaster.Application.Features.Campaigns.Commands.JoinCampaign;

/// <summary>
/// Command to join a campaign using a join code. The user becomes a Player.
/// </summary>
/// <param name="JoinCode">The 8-character join code for the campaign.</param>
public record JoinCampaignCommand(string JoinCode) : IRequest<CampaignDto>;
