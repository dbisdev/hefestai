using Loremaster.Application.Common.Interfaces;
using Loremaster.Application.Features.Admin.DTOs;
using Loremaster.Domain.Exceptions;
using MediatR;

namespace Loremaster.Application.Features.Admin.Queries.GetCampaignById;

/// <summary>
/// Handler for GetAdminCampaignByIdQuery. Returns a specific campaign for admin management.
/// </summary>
public class GetAdminCampaignByIdQueryHandler : IRequestHandler<GetAdminCampaignByIdQuery, AdminCampaignDto>
{
    private readonly ICampaignRepository _campaignRepository;

    public GetAdminCampaignByIdQueryHandler(ICampaignRepository campaignRepository)
    {
        _campaignRepository = campaignRepository;
    }

    /// <summary>
    /// Handles the get campaign by ID query.
    /// </summary>
    /// <param name="request">The query containing the campaign ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Campaign as AdminCampaignDto.</returns>
    /// <exception cref="NotFoundException">Thrown when campaign not found.</exception>
    public async Task<AdminCampaignDto> Handle(
        GetAdminCampaignByIdQuery request,
        CancellationToken cancellationToken)
    {
        var campaign = await _campaignRepository.GetByIdWithDetailsAsync(request.CampaignId, cancellationToken)
            ?? throw new NotFoundException("Campaign", request.CampaignId);

        return new AdminCampaignDto
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
        };
    }
}
