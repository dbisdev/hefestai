using Loremaster.Application.Common.Interfaces;
using Loremaster.Domain.Exceptions;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Loremaster.Application.Features.Campaigns.Commands.DeleteCampaign;

/// <summary>
/// Handler for DeleteCampaignCommand. Deletes a campaign (Owner only).
/// </summary>
public class DeleteCampaignCommandHandler : IRequestHandler<DeleteCampaignCommand, Unit>
{
    private readonly ICampaignRepository _campaignRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<DeleteCampaignCommandHandler> _logger;

    public DeleteCampaignCommandHandler(
        ICampaignRepository campaignRepository,
        ICurrentUserService currentUserService,
        IUnitOfWork unitOfWork,
        ILogger<DeleteCampaignCommandHandler> logger)
    {
        _campaignRepository = campaignRepository;
        _currentUserService = currentUserService;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    /// <summary>
    /// Handles the campaign deletion command.
    /// </summary>
    /// <param name="request">The delete campaign command.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Unit value.</returns>
    /// <exception cref="ForbiddenAccessException">Thrown when user is not authenticated or not the owner.</exception>
    /// <exception cref="NotFoundException">Thrown when campaign doesn't exist.</exception>
    public async Task<Unit> Handle(DeleteCampaignCommand request, CancellationToken cancellationToken)
    {
        if (!_currentUserService.UserId.HasValue)
        {
            throw new ForbiddenAccessException("User must be authenticated to delete a campaign");
        }

        var userId = _currentUserService.UserId.Value;

        var campaign = await _campaignRepository.GetByIdAsync(request.CampaignId, cancellationToken);
        if (campaign == null)
        {
            throw new NotFoundException("Campaign", request.CampaignId);
        }

        // Only owner can delete
        if (campaign.OwnerId != userId)
        {
            throw new ForbiddenAccessException("Only the campaign owner can delete the campaign");
        }

        _campaignRepository.Delete(campaign);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Campaign {CampaignId} deleted by user {UserId}", request.CampaignId, userId);

        return Unit.Value;
    }
}
