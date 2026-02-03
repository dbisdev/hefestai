using Loremaster.Application.Common.Interfaces;
using Loremaster.Application.Features.Campaigns.DTOs;
using Loremaster.Domain.Exceptions;
using MediatR;

namespace Loremaster.Application.Features.Campaigns.Queries.GetMyCampaigns;

/// <summary>
/// Handler for GetMyCampaignsQuery. Returns all campaigns the user is a member of.
/// </summary>
public class GetMyCampaignsQueryHandler : IRequestHandler<GetMyCampaignsQuery, IEnumerable<CampaignDto>>
{
    private readonly ICampaignRepository _campaignRepository;
    private readonly ICampaignMemberRepository _campaignMemberRepository;
    private readonly ICurrentUserService _currentUserService;

    public GetMyCampaignsQueryHandler(
        ICampaignRepository campaignRepository,
        ICampaignMemberRepository campaignMemberRepository,
        ICurrentUserService currentUserService)
    {
        _campaignRepository = campaignRepository;
        _campaignMemberRepository = campaignMemberRepository;
        _currentUserService = currentUserService;
    }

    /// <summary>
    /// Handles the get my campaigns query.
    /// </summary>
    /// <param name="request">The query (no parameters).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of campaigns the user is a member of.</returns>
    /// <exception cref="ForbiddenAccessException">Thrown when user is not authenticated.</exception>
    public async Task<IEnumerable<CampaignDto>> Handle(GetMyCampaignsQuery request, CancellationToken cancellationToken)
    {
        if (!_currentUserService.UserId.HasValue)
        {
            throw new ForbiddenAccessException("User must be authenticated to view campaigns");
        }

        var userId = _currentUserService.UserId.Value;

        var campaigns = await _campaignRepository.GetByUserIdAsync(userId, cancellationToken);

        var dtos = new List<CampaignDto>();
        foreach (var campaign in campaigns)
        {
            var membership = await _campaignMemberRepository
                .GetByCampaignAndUserAsync(campaign.Id, userId, cancellationToken);
            dtos.Add(CampaignDto.FromEntity(campaign, membership?.Role));
        }

        return dtos;
    }
}
