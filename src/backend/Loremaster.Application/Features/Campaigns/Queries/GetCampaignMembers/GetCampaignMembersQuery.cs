using Loremaster.Application.Features.Campaigns.DTOs;
using MediatR;

namespace Loremaster.Application.Features.Campaigns.Queries.GetCampaignMembers;

/// <summary>
/// Query to get all members of a campaign.
/// Only campaign members can view the member list.
/// </summary>
/// <param name="CampaignId">The ID of the campaign.</param>
public record GetCampaignMembersQuery(Guid CampaignId) : IRequest<IEnumerable<CampaignMemberDto>>;
