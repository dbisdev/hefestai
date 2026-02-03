using Loremaster.Application.Common.Interfaces;
using Loremaster.Domain.Exceptions;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Loremaster.Application.Features.Campaigns.Commands.RemoveMember;

/// <summary>
/// Handler for RemoveMemberCommand. Removes a member from a campaign (Master only).
/// </summary>
public class RemoveMemberCommandHandler : IRequestHandler<RemoveMemberCommand, Unit>
{
    private readonly ICampaignRepository _campaignRepository;
    private readonly ICampaignMemberRepository _campaignMemberRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<RemoveMemberCommandHandler> _logger;

    public RemoveMemberCommandHandler(
        ICampaignRepository campaignRepository,
        ICampaignMemberRepository campaignMemberRepository,
        ICurrentUserService currentUserService,
        IUnitOfWork unitOfWork,
        ILogger<RemoveMemberCommandHandler> logger)
    {
        _campaignRepository = campaignRepository;
        _campaignMemberRepository = campaignMemberRepository;
        _currentUserService = currentUserService;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    /// <summary>
    /// Handles the remove member command.
    /// </summary>
    /// <param name="request">The remove member command.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Unit value.</returns>
    /// <exception cref="ForbiddenAccessException">Thrown when user is not authenticated or not a Master.</exception>
    /// <exception cref="NotFoundException">Thrown when member doesn't exist.</exception>
    /// <exception cref="DomainException">Thrown when trying to remove the owner.</exception>
    public async Task<Unit> Handle(RemoveMemberCommand request, CancellationToken cancellationToken)
    {
        if (!_currentUserService.UserId.HasValue)
        {
            throw new ForbiddenAccessException("User must be authenticated to remove members");
        }

        var userId = _currentUserService.UserId.Value;

        // Only Masters can remove members
        var isMaster = await _campaignMemberRepository.IsMasterAsync(request.CampaignId, userId, cancellationToken);
        if (!isMaster)
        {
            throw new ForbiddenAccessException("Only campaign masters can remove members");
        }

        var member = await _campaignMemberRepository.GetByIdAsync(request.MemberId, cancellationToken);
        if (member == null || member.CampaignId != request.CampaignId)
        {
            throw new NotFoundException("Member", request.MemberId);
        }

        var campaign = await _campaignRepository.GetByIdAsync(request.CampaignId, cancellationToken);

        // Cannot remove the owner
        if (campaign != null && member.UserId == campaign.OwnerId)
        {
            throw new DomainException("Cannot remove the campaign owner");
        }

        _campaignMemberRepository.Delete(member);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Member {MemberId} removed from campaign {CampaignId} by user {UserId}",
            request.MemberId, request.CampaignId, userId);

        return Unit.Value;
    }
}
