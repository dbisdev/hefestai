using Loremaster.Application.Common.Interfaces;
using Loremaster.Application.Features.Campaigns.DTOs;
using Loremaster.Domain.Enums;
using Loremaster.Domain.Exceptions;
using MediatR;

namespace Loremaster.Application.Features.Campaigns.Commands.UpdateCampaignStatus;

/// <summary>
/// Handler for UpdateCampaignStatusCommand. Activates or deactivates a campaign (Master only).
/// </summary>
public class UpdateCampaignStatusCommandHandler : IRequestHandler<UpdateCampaignStatusCommand, CampaignDto>
{
    private readonly ICampaignRepository _campaignRepository;
    private readonly ICampaignMemberRepository _campaignMemberRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateCampaignStatusCommandHandler(
        ICampaignRepository campaignRepository,
        ICampaignMemberRepository campaignMemberRepository,
        ICurrentUserService currentUserService,
        IUnitOfWork unitOfWork)
    {
        _campaignRepository = campaignRepository;
        _campaignMemberRepository = campaignMemberRepository;
        _currentUserService = currentUserService;
        _unitOfWork = unitOfWork;
    }

    /// <summary>
    /// Handles the campaign status update command.
    /// </summary>
    /// <param name="request">The update status command.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The updated campaign.</returns>
    /// <exception cref="ForbiddenAccessException">Thrown when user is not authenticated or not a Master.</exception>
    /// <exception cref="NotFoundException">Thrown when campaign doesn't exist.</exception>
    public async Task<CampaignDto> Handle(UpdateCampaignStatusCommand request, CancellationToken cancellationToken)
    {
        if (!_currentUserService.UserId.HasValue)
        {
            throw new ForbiddenAccessException("User must be authenticated to change campaign status");
        }

        var userId = _currentUserService.UserId.Value;

        var campaign = await _campaignRepository.GetByIdAsync(request.CampaignId, cancellationToken);
        if (campaign == null)
        {
            throw new NotFoundException("Campaign", request.CampaignId);
        }

        // Only Masters can change status
        var isMaster = await _campaignMemberRepository.IsMasterAsync(request.CampaignId, userId, cancellationToken);
        if (!isMaster)
        {
            throw new ForbiddenAccessException("Only campaign masters can change campaign status");
        }

        if (request.IsActive)
        {
            campaign.Activate();
        }
        else
        {
            campaign.Deactivate();
        }

        _campaignRepository.Update(campaign);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return CampaignDto.FromEntity(campaign, CampaignRole.Master);
    }
}
