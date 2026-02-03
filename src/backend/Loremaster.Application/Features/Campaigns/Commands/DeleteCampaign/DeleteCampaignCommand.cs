using MediatR;

namespace Loremaster.Application.Features.Campaigns.Commands.DeleteCampaign;

/// <summary>
/// Command to delete a campaign. Only the Owner can delete.
/// </summary>
/// <param name="CampaignId">The ID of the campaign to delete.</param>
public record DeleteCampaignCommand(Guid CampaignId) : IRequest<Unit>;
