using Loremaster.Application.Common.Interfaces;
using Loremaster.Application.Features.Admin.DTOs;
using Loremaster.Domain.Exceptions;
using MediatR;

namespace Loremaster.Application.Features.Admin.Commands.UpdateCampaign;

/// <summary>
/// Handler for UpdateAdminCampaignCommand. Updates an existing campaign.
/// </summary>
public class UpdateAdminCampaignCommandHandler : IRequestHandler<UpdateAdminCampaignCommand, AdminCampaignDto>
{
    private readonly ICampaignRepository _campaignRepository;
    private readonly IUserRepository _userRepository;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateAdminCampaignCommandHandler(
        ICampaignRepository campaignRepository,
        IUserRepository userRepository,
        IUnitOfWork unitOfWork)
    {
        _campaignRepository = campaignRepository;
        _userRepository = userRepository;
        _unitOfWork = unitOfWork;
    }

    /// <summary>
    /// Handles the update campaign command.
    /// </summary>
    /// <param name="request">The command with updated campaign data.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Updated campaign as AdminCampaignDto.</returns>
    /// <exception cref="NotFoundException">Thrown when campaign or new owner not found.</exception>
    public async Task<AdminCampaignDto> Handle(
        UpdateAdminCampaignCommand request,
        CancellationToken cancellationToken)
    {
        var campaign = await _campaignRepository.GetByIdWithDetailsAsync(request.CampaignId, cancellationToken)
            ?? throw new NotFoundException("Campaign", request.CampaignId);

        // Update name and description if provided
        if (request.Name != null || request.Description != null)
        {
            campaign.Update(
                request.Name ?? campaign.Name,
                request.Description ?? campaign.Description
            );
        }

        // Update active status if provided
        if (request.IsActive.HasValue)
        {
            if (request.IsActive.Value)
                campaign.Activate();
            else
                campaign.Deactivate();
        }

        // Transfer ownership if new owner ID provided
        if (request.OwnerId.HasValue && request.OwnerId.Value != campaign.OwnerId)
        {
            var newOwner = await _userRepository.GetByIdAsync(request.OwnerId.Value, cancellationToken)
                ?? throw new NotFoundException("User", request.OwnerId.Value);
            
            campaign.TransferOwnership(request.OwnerId.Value);
        }

        _campaignRepository.Update(campaign);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Reload to get fresh data
        campaign = await _campaignRepository.GetByIdWithDetailsAsync(request.CampaignId, cancellationToken);

        return new AdminCampaignDto
        {
            Id = campaign!.Id,
            Name = campaign.Name,
            Description = campaign.Description,
            JoinCode = campaign.JoinCode,
            IsActive = campaign.IsActive,
            OwnerId = campaign.OwnerId,
            OwnerName = campaign.Owner?.DisplayName ?? campaign.Owner?.Email ?? "Unknown",
            GameSystemId = campaign.GameSystemId,
            GameSystemName = campaign.GameSystem?.Name ?? "Unknown",
            MemberCount = campaign.Members.Count,
            EntityCount = campaign.LoreEntities.Count,
            CreatedAt = campaign.CreatedAt,
            UpdatedAt = campaign.UpdatedAt
        };
    }
}
