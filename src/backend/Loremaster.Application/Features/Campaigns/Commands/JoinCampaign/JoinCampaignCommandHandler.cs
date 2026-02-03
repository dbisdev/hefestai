using Loremaster.Application.Common.Interfaces;
using Loremaster.Application.Features.Campaigns.DTOs;
using Loremaster.Domain.Entities;
using Loremaster.Domain.Enums;
using Loremaster.Domain.Exceptions;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Loremaster.Application.Features.Campaigns.Commands.JoinCampaign;

/// <summary>
/// Handler for JoinCampaignCommand. Joins a campaign using a join code (as Player).
/// </summary>
public class JoinCampaignCommandHandler : IRequestHandler<JoinCampaignCommand, CampaignDto>
{
    private readonly ICampaignRepository _campaignRepository;
    private readonly ICampaignMemberRepository _campaignMemberRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<JoinCampaignCommandHandler> _logger;

    public JoinCampaignCommandHandler(
        ICampaignRepository campaignRepository,
        ICampaignMemberRepository campaignMemberRepository,
        ICurrentUserService currentUserService,
        IUnitOfWork unitOfWork,
        ILogger<JoinCampaignCommandHandler> logger)
    {
        _campaignRepository = campaignRepository;
        _campaignMemberRepository = campaignMemberRepository;
        _currentUserService = currentUserService;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    /// <summary>
    /// Handles the join campaign command.
    /// </summary>
    /// <param name="request">The join campaign command.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The campaign the user joined.</returns>
    /// <exception cref="ForbiddenAccessException">Thrown when user is not authenticated.</exception>
    /// <exception cref="NotFoundException">Thrown when join code is invalid.</exception>
    /// <exception cref="DomainException">Thrown when campaign is inactive or user is already a member.</exception>
    public async Task<CampaignDto> Handle(JoinCampaignCommand request, CancellationToken cancellationToken)
    {
        if (!_currentUserService.UserId.HasValue)
        {
            throw new ForbiddenAccessException("User must be authenticated to join a campaign");
        }

        var userId = _currentUserService.UserId.Value;

        var campaign = await _campaignRepository.GetByJoinCodeAsync(request.JoinCode, cancellationToken);
        if (campaign == null)
        {
            throw new NotFoundException("Campaign with join code", request.JoinCode);
        }

        if (!campaign.IsActive)
        {
            throw new DomainException("This campaign is no longer accepting new members");
        }

        // Check if already a member
        var existingMembership = await _campaignMemberRepository
            .GetByCampaignAndUserAsync(campaign.Id, userId, cancellationToken);
        if (existingMembership != null)
        {
            throw new DomainException("You are already a member of this campaign");
        }

        // Join as Player
        var membership = CampaignMember.Create(campaign.Id, userId, CampaignRole.Player);
        await _campaignMemberRepository.AddAsync(membership, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("User {UserId} joined campaign {CampaignId}", userId, campaign.Id);

        return CampaignDto.FromEntity(campaign, CampaignRole.Player);
    }
}
