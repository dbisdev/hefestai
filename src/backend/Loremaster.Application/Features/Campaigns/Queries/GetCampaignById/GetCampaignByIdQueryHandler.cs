using Loremaster.Application.Common.Interfaces;
using Loremaster.Application.Features.Campaigns.DTOs;
using Loremaster.Domain.Exceptions;
using MediatR;

namespace Loremaster.Application.Features.Campaigns.Queries.GetCampaignById;

/// <summary>
/// Handler for GetCampaignByIdQuery. Returns detailed campaign information.
/// Only members can view campaign details.
/// </summary>
public class GetCampaignByIdQueryHandler : IRequestHandler<GetCampaignByIdQuery, CampaignDetailDto>
{
    private readonly ICampaignRepository _campaignRepository;
    private readonly ICampaignMemberRepository _campaignMemberRepository;
    private readonly ICurrentUserService _currentUserService;

    public GetCampaignByIdQueryHandler(
        ICampaignRepository campaignRepository,
        ICampaignMemberRepository campaignMemberRepository,
        ICurrentUserService currentUserService)
    {
        _campaignRepository = campaignRepository;
        _campaignMemberRepository = campaignMemberRepository;
        _currentUserService = currentUserService;
    }

    /// <summary>
    /// Handles the get campaign by ID query.
    /// </summary>
    /// <param name="request">The query with campaign ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Detailed campaign information.</returns>
    /// <exception cref="ForbiddenAccessException">Thrown when user is not authenticated or not a member.</exception>
    /// <exception cref="NotFoundException">Thrown when campaign doesn't exist.</exception>
    public async Task<CampaignDetailDto> Handle(GetCampaignByIdQuery request, CancellationToken cancellationToken)
    {
        if (!_currentUserService.UserId.HasValue)
        {
            throw new ForbiddenAccessException("User must be authenticated to view campaign details");
        }

        var userId = _currentUserService.UserId.Value;

        var campaign = await _campaignRepository.GetByIdWithMembersAsync(request.CampaignId, cancellationToken);
        if (campaign == null)
        {
            throw new NotFoundException("Campaign", request.CampaignId);
        }

        // Check if user is a member
        var isMember = await _campaignMemberRepository.IsMemberAsync(request.CampaignId, userId, cancellationToken);
        if (!isMember)
        {
            throw new ForbiddenAccessException("You are not a member of this campaign");
        }

        var membership = await _campaignMemberRepository
            .GetByCampaignAndUserAsync(request.CampaignId, userId, cancellationToken);
        var memberCount = await _campaignMemberRepository.GetMemberCountAsync(request.CampaignId, cancellationToken);

        return CampaignDetailDto.FromEntity(campaign, membership?.Role, memberCount);
    }
}
