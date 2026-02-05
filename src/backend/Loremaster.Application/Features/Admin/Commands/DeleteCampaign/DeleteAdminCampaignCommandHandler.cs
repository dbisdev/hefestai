using Loremaster.Application.Common.Interfaces;
using Loremaster.Domain.Exceptions;
using MediatR;

namespace Loremaster.Application.Features.Admin.Commands.DeleteCampaign;

/// <summary>
/// Handler for DeleteAdminCampaignCommand. Soft deletes a campaign.
/// </summary>
public class DeleteAdminCampaignCommandHandler : IRequestHandler<DeleteAdminCampaignCommand, Unit>
{
    private readonly ICampaignRepository _campaignRepository;
    private readonly IUnitOfWork _unitOfWork;

    public DeleteAdminCampaignCommandHandler(
        ICampaignRepository campaignRepository,
        IUnitOfWork unitOfWork)
    {
        _campaignRepository = campaignRepository;
        _unitOfWork = unitOfWork;
    }

    /// <summary>
    /// Handles the delete campaign command.
    /// </summary>
    /// <param name="request">The command containing the campaign ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Unit.</returns>
    /// <exception cref="NotFoundException">Thrown when campaign not found.</exception>
    public async Task<Unit> Handle(
        DeleteAdminCampaignCommand request,
        CancellationToken cancellationToken)
    {
        var campaign = await _campaignRepository.GetByIdAsync(request.CampaignId, cancellationToken)
            ?? throw new NotFoundException("Campaign", request.CampaignId);

        // Soft delete
        _campaignRepository.Delete(campaign);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Unit.Value;
    }
}
