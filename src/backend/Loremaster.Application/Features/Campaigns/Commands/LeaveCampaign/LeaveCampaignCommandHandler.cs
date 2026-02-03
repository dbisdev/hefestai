using Loremaster.Application.Common.Interfaces;
using Loremaster.Domain.Exceptions;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Loremaster.Application.Features.Campaigns.Commands.LeaveCampaign;

/// <summary>
/// Handler for LeaveCampaignCommand. Leaves a campaign (Owner cannot leave).
/// </summary>
public class LeaveCampaignCommandHandler : IRequestHandler<LeaveCampaignCommand, Unit>
{
    private readonly ICampaignRepository _campaignRepository;
    private readonly ICampaignMemberRepository _campaignMemberRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<LeaveCampaignCommandHandler> _logger;

    public LeaveCampaignCommandHandler(
        ICampaignRepository campaignRepository,
        ICampaignMemberRepository campaignMemberRepository,
        ICurrentUserService currentUserService,
        IUnitOfWork unitOfWork,
        ILogger<LeaveCampaignCommandHandler> logger)
    {
        _campaignRepository = campaignRepository;
        _campaignMemberRepository = campaignMemberRepository;
        _currentUserService = currentUserService;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    /// <summary>
    /// Handles the leave campaign command.
    /// </summary>
    /// <param name="request">The leave campaign command.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Unit value.</returns>
    /// <exception cref="ForbiddenAccessException">Thrown when user is not authenticated.</exception>
    /// <exception cref="NotFoundException">Thrown when campaign or membership doesn't exist.</exception>
    /// <exception cref="DomainException">Thrown when the owner tries to leave.</exception>
    public async Task<Unit> Handle(LeaveCampaignCommand request, CancellationToken cancellationToken)
    {
        if (!_currentUserService.UserId.HasValue)
        {
            throw new ForbiddenAccessException("User must be authenticated to leave a campaign");
        }

        var userId = _currentUserService.UserId.Value;

        var campaign = await _campaignRepository.GetByIdAsync(request.CampaignId, cancellationToken);
        if (campaign == null)
        {
            throw new NotFoundException("Campaign", request.CampaignId);
        }

        // Owner cannot leave (must delete or transfer ownership)
        if (campaign.OwnerId == userId)
        {
            throw new DomainException("Campaign owner cannot leave. Transfer ownership or delete the campaign instead.");
        }

        var membership = await _campaignMemberRepository
            .GetByCampaignAndUserAsync(request.CampaignId, userId, cancellationToken);
        if (membership == null)
        {
            throw new NotFoundException("Membership", $"User {userId} in Campaign {request.CampaignId}");
        }

        _campaignMemberRepository.Delete(membership);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("User {UserId} left campaign {CampaignId}", userId, request.CampaignId);

        return Unit.Value;
    }
}
