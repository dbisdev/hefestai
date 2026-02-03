using System.Text.Json;
using Loremaster.Application.Common.Interfaces;
using Loremaster.Application.Features.Campaigns.DTOs;
using Loremaster.Domain.Entities;
using Loremaster.Domain.Enums;
using Loremaster.Domain.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Loremaster.Application.Features.Campaigns.Commands.CreateCampaign;

/// <summary>
/// Handler for CreateCampaignCommand. Creates a new campaign and adds the creator as Master.
/// </summary>
public class CreateCampaignCommandHandler : IRequestHandler<CreateCampaignCommand, CampaignDetailDto>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ICampaignRepository _campaignRepository;
    private readonly ICampaignMemberRepository _campaignMemberRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<CreateCampaignCommandHandler> _logger;

    public CreateCampaignCommandHandler(
        IApplicationDbContext dbContext,
        ICampaignRepository campaignRepository,
        ICampaignMemberRepository campaignMemberRepository,
        ICurrentUserService currentUserService,
        IUnitOfWork unitOfWork,
        ILogger<CreateCampaignCommandHandler> logger)
    {
        _dbContext = dbContext;
        _campaignRepository = campaignRepository;
        _campaignMemberRepository = campaignMemberRepository;
        _currentUserService = currentUserService;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    /// <summary>
    /// Handles the campaign creation command.
    /// </summary>
    /// <param name="request">The create campaign command.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The created campaign details.</returns>
    /// <exception cref="ForbiddenAccessException">Thrown when user is not authenticated.</exception>
    /// <exception cref="DomainException">Thrown when game system is invalid.</exception>
    public async Task<CampaignDetailDto> Handle(CreateCampaignCommand request, CancellationToken cancellationToken)
    {
        if (!_currentUserService.UserId.HasValue)
        {
            throw new ForbiddenAccessException("User must be authenticated to create a campaign");
        }

        var userId = _currentUserService.UserId.Value;

        // Validate game system exists
        var gameSystemExists = await _dbContext.GameSystems
            .AnyAsync(gs => gs.Id == request.GameSystemId, cancellationToken);
        
        if (!gameSystemExists)
        {
            throw new DomainException("Invalid game system");
        }

        // Parse settings if provided
        JsonDocument? settings = null;
        if (request.Settings != null)
        {
            settings = JsonDocument.Parse(JsonSerializer.Serialize(request.Settings));
        }

        // Create the campaign
        var campaign = Campaign.Create(
            ownerId: userId,
            gameSystemId: request.GameSystemId,
            name: request.Name,
            description: request.Description,
            settings: settings
        );

        await _campaignRepository.AddAsync(campaign, cancellationToken);

        // Add creator as Master
        var membership = CampaignMember.Create(campaign.Id, userId, CampaignRole.Master);
        await _campaignMemberRepository.AddAsync(membership, cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Campaign {CampaignId} created by user {UserId}", campaign.Id, userId);

        return CampaignDetailDto.FromEntity(campaign, CampaignRole.Master, memberCount: 1);
    }
}
