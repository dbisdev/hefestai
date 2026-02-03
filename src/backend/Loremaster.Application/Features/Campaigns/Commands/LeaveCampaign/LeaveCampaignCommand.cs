using MediatR;

namespace Loremaster.Application.Features.Campaigns.Commands.LeaveCampaign;

/// <summary>
/// Command to leave a campaign. The Owner cannot leave (must delete or transfer ownership).
/// </summary>
/// <param name="CampaignId">The ID of the campaign to leave.</param>
public record LeaveCampaignCommand(Guid CampaignId) : IRequest<Unit>;
