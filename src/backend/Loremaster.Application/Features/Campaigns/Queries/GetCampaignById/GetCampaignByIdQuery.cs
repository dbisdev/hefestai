using Loremaster.Application.Features.Campaigns.DTOs;
using MediatR;

namespace Loremaster.Application.Features.Campaigns.Queries.GetCampaignById;

/// <summary>
/// Query to get detailed information about a specific campaign.
/// Only members can view campaign details.
/// </summary>
/// <param name="CampaignId">The ID of the campaign to retrieve.</param>
public record GetCampaignByIdQuery(Guid CampaignId) : IRequest<CampaignDetailDto>;
