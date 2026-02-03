using System.Text.Json;
using Loremaster.Application.Common.Interfaces;
using Loremaster.Application.Features.Campaigns.DTOs;
using Loremaster.Domain.Enums;
using Loremaster.Domain.Exceptions;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Loremaster.Application.Features.Campaigns.Commands.UpdateCampaign;

/// <summary>
/// Handler for UpdateCampaignCommand. Updates campaign details (Master only).
/// </summary>
public class UpdateCampaignCommandHandler : IRequestHandler<UpdateCampaignCommand, CampaignDetailDto>
{
    private readonly ICampaignRepository _campaignRepository;
    private readonly ICampaignMemberRepository _campaignMemberRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<UpdateCampaignCommandHandler> _logger;

    public UpdateCampaignCommandHandler(
        ICampaignRepository campaignRepository,
        ICampaignMemberRepository campaignMemberRepository,
        ICurrentUserService currentUserService,
        IUnitOfWork unitOfWork,
        ILogger<UpdateCampaignCommandHandler> logger)
    {
        _campaignRepository = campaignRepository;
        _campaignMemberRepository = campaignMemberRepository;
        _currentUserService = currentUserService;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    /// <summary>
    /// Handles the campaign update command.
    /// </summary>
    /// <param name="request">The update campaign command.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The updated campaign details.</returns>
    /// <exception cref="ForbiddenAccessException">Thrown when user is not authenticated or not a Master.</exception>
    /// <exception cref="NotFoundException">Thrown when campaign doesn't exist.</exception>
    public async Task<CampaignDetailDto> Handle(UpdateCampaignCommand request, CancellationToken cancellationToken)
    {
        if (!_currentUserService.UserId.HasValue)
        {
            throw new ForbiddenAccessException("User must be authenticated to update a campaign");
        }

        var userId = _currentUserService.UserId.Value;

        var campaign = await _campaignRepository.GetByIdAsync(request.CampaignId, cancellationToken);
        if (campaign == null)
        {
            throw new NotFoundException("Campaign", request.CampaignId);
        }

        // Only Masters can update campaigns
        var isMaster = await _campaignMemberRepository.IsMasterAsync(request.CampaignId, userId, cancellationToken);
        if (!isMaster)
        {
            throw new ForbiddenAccessException("Only campaign masters can update campaign details");
        }

        // Parse settings if provided
        JsonDocument? settings = null;
        if (request.Settings != null)
        {
            settings = JsonDocument.Parse(JsonSerializer.Serialize(request.Settings));
        }

        // Update the campaign
        campaign.Update(request.Name, request.Description, settings);

        _campaignRepository.Update(campaign);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Campaign {CampaignId} updated by user {UserId}", request.CampaignId, userId);

        var memberCount = await _campaignMemberRepository.GetMemberCountAsync(request.CampaignId, cancellationToken);
        return CampaignDetailDto.FromEntity(campaign, CampaignRole.Master, memberCount);
    }
}
