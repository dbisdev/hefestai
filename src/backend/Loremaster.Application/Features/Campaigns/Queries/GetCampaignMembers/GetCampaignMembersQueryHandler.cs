using Loremaster.Application.Common.Interfaces;
using Loremaster.Application.Features.Campaigns.DTOs;
using Loremaster.Domain.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Loremaster.Application.Features.Campaigns.Queries.GetCampaignMembers;

/// <summary>
/// Handler for GetCampaignMembersQuery. Returns all members of a campaign.
/// Only campaign members can view the member list.
/// </summary>
public class GetCampaignMembersQueryHandler : IRequestHandler<GetCampaignMembersQuery, IEnumerable<CampaignMemberDto>>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ICampaignRepository _campaignRepository;
    private readonly ICampaignMemberRepository _campaignMemberRepository;
    private readonly ICurrentUserService _currentUserService;

    public GetCampaignMembersQueryHandler(
        IApplicationDbContext dbContext,
        ICampaignRepository campaignRepository,
        ICampaignMemberRepository campaignMemberRepository,
        ICurrentUserService currentUserService)
    {
        _dbContext = dbContext;
        _campaignRepository = campaignRepository;
        _campaignMemberRepository = campaignMemberRepository;
        _currentUserService = currentUserService;
    }

    /// <summary>
    /// Handles the get campaign members query.
    /// </summary>
    /// <param name="request">The query with campaign ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of campaign members.</returns>
    /// <exception cref="ForbiddenAccessException">Thrown when user is not authenticated or not a member.</exception>
    /// <exception cref="NotFoundException">Thrown when campaign doesn't exist.</exception>
    public async Task<IEnumerable<CampaignMemberDto>> Handle(
        GetCampaignMembersQuery request, 
        CancellationToken cancellationToken)
    {
        if (!_currentUserService.UserId.HasValue)
        {
            throw new ForbiddenAccessException("User must be authenticated to view campaign members");
        }

        var userId = _currentUserService.UserId.Value;

        var campaign = await _campaignRepository.GetByIdAsync(request.CampaignId, cancellationToken);
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

        // Get members with user information
        var members = await _dbContext.CampaignMembers
            .Include(cm => cm.User)
            .Where(cm => cm.CampaignId == request.CampaignId)
            .ToListAsync(cancellationToken);

        return members.Select(CampaignMemberDto.FromEntity);
    }
}
