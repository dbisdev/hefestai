using Loremaster.Application.Features.Campaigns.DTOs;
using MediatR;

namespace Loremaster.Application.Features.Campaigns.Queries.GetMyCampaigns;

/// <summary>
/// Query to get all campaigns the current user is a member of.
/// </summary>
public record GetMyCampaignsQuery : IRequest<IEnumerable<CampaignDto>>;
