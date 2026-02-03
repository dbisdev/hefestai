using Loremaster.Application.Features.Campaigns.DTOs;
using MediatR;

namespace Loremaster.Application.Features.Campaigns.Commands.RegenerateJoinCode;

/// <summary>
/// Command to regenerate the join code for a campaign. Only Masters can regenerate.
/// </summary>
/// <param name="CampaignId">The ID of the campaign.</param>
public record RegenerateJoinCodeCommand(Guid CampaignId) : IRequest<JoinCodeDto>;
