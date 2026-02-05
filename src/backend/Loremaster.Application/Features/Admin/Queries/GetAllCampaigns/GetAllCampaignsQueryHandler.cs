using Loremaster.Application.Common.Interfaces;
using Loremaster.Application.Features.Admin.DTOs;
using MediatR;

namespace Loremaster.Application.Features.Admin.Queries.GetAllCampaigns;

/// <summary>
/// Handler for GetAllCampaignsQuery. Returns all campaigns for admin management.
/// </summary>
public class GetAllCampaignsQueryHandler : IRequestHandler<GetAllCampaignsQuery, IEnumerable<AdminCampaignDto>>
{
    private readonly ICampaignRepository _campaignRepository;

    public GetAllCampaignsQueryHandler(ICampaignRepository campaignRepository)
    {
        _campaignRepository = campaignRepository;
    }

    /// <summary>
    /// Handles the get all campaigns query.
    /// </summary>
    /// <param name="request">The query with optional filters.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Collection of campaigns as AdminCampaignDto.</returns>
    public async Task<IEnumerable<AdminCampaignDto>> Handle(
        GetAllCampaignsQuery request,
        CancellationToken cancellationToken)
    {
        var campaigns = await _campaignRepository.GetAllWithDetailsAsync(request.IncludeInactive, cancellationToken);

        return campaigns.Select(campaign => new AdminCampaignDto
        {
            Id = campaign.Id,
            Name = campaign.Name,
            Description = campaign.Description,
            JoinCode = campaign.JoinCode,
            IsActive = campaign.IsActive,
            OwnerId = campaign.OwnerId,
            OwnerName = campaign.Owner?.DisplayName ?? campaign.Owner?.Email ?? "Unknown",
            GameSystemId = campaign.GameSystemId,
            GameSystemName = campaign.GameSystem?.Name ?? "Unknown",
            MemberCount = campaign.Members.Count,
            EntityCount = campaign.LoreEntities.Count,
            CreatedAt = campaign.CreatedAt,
            UpdatedAt = campaign.UpdatedAt
        });
    }
}
