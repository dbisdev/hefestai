using Loremaster.Application.Common.Interfaces;
using Loremaster.Application.Features.Campaigns.DTOs;
using Loremaster.Domain.Exceptions;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Loremaster.Application.Features.Campaigns.Commands.RegenerateJoinCode;

/// <summary>
/// Handler for RegenerateJoinCodeCommand. Regenerates the join code (Master only).
/// </summary>
public class RegenerateJoinCodeCommandHandler : IRequestHandler<RegenerateJoinCodeCommand, JoinCodeDto>
{
    private readonly ICampaignRepository _campaignRepository;
    private readonly ICampaignMemberRepository _campaignMemberRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<RegenerateJoinCodeCommandHandler> _logger;

    public RegenerateJoinCodeCommandHandler(
        ICampaignRepository campaignRepository,
        ICampaignMemberRepository campaignMemberRepository,
        ICurrentUserService currentUserService,
        IUnitOfWork unitOfWork,
        ILogger<RegenerateJoinCodeCommandHandler> logger)
    {
        _campaignRepository = campaignRepository;
        _campaignMemberRepository = campaignMemberRepository;
        _currentUserService = currentUserService;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    /// <summary>
    /// Handles the regenerate join code command.
    /// </summary>
    /// <param name="request">The regenerate join code command.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The new join code.</returns>
    /// <exception cref="ForbiddenAccessException">Thrown when user is not authenticated or not a Master.</exception>
    /// <exception cref="NotFoundException">Thrown when campaign doesn't exist.</exception>
    public async Task<JoinCodeDto> Handle(RegenerateJoinCodeCommand request, CancellationToken cancellationToken)
    {
        if (!_currentUserService.UserId.HasValue)
        {
            throw new ForbiddenAccessException("User must be authenticated to regenerate join code");
        }

        var userId = _currentUserService.UserId.Value;

        var campaign = await _campaignRepository.GetByIdAsync(request.CampaignId, cancellationToken);
        if (campaign == null)
        {
            throw new NotFoundException("Campaign", request.CampaignId);
        }

        // Only Masters can regenerate the join code
        var isMaster = await _campaignMemberRepository.IsMasterAsync(request.CampaignId, userId, cancellationToken);
        if (!isMaster)
        {
            throw new ForbiddenAccessException("Only campaign masters can regenerate the join code");
        }

        campaign.RegenerateJoinCode();
        _campaignRepository.Update(campaign);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Join code regenerated for campaign {CampaignId} by user {UserId}", 
            request.CampaignId, userId);

        return JoinCodeDto.FromCampaign(campaign);
    }
}
