using Loremaster.Application.Common.Interfaces;
using Loremaster.Application.Features.LoreEntities.DTOs;
using Loremaster.Domain.Enums;
using Loremaster.Domain.Exceptions;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Loremaster.Application.Features.LoreEntities.Commands.TransferEntityOwnership;

/// <summary>
/// Handler for TransferEntityOwnershipCommand. Transfers entity ownership to another campaign member.
/// Only the campaign master can transfer entity ownership.
/// </summary>
public class TransferEntityOwnershipCommandHandler : IRequestHandler<TransferEntityOwnershipCommand, LoreEntityDto>
{
    private readonly ILoreEntityRepository _loreEntityRepository;
    private readonly ICampaignMemberRepository _campaignMemberRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<TransferEntityOwnershipCommandHandler> _logger;

    /// <summary>
    /// Initializes a new instance of the TransferEntityOwnershipCommandHandler.
    /// </summary>
    /// <param name="loreEntityRepository">Repository for lore entities.</param>
    /// <param name="campaignMemberRepository">Repository for campaign members.</param>
    /// <param name="currentUserService">Service to get current user info.</param>
    /// <param name="unitOfWork">Unit of work for persistence.</param>
    /// <param name="logger">Logger instance.</param>
    public TransferEntityOwnershipCommandHandler(
        ILoreEntityRepository loreEntityRepository,
        ICampaignMemberRepository campaignMemberRepository,
        ICurrentUserService currentUserService,
        IUnitOfWork unitOfWork,
        ILogger<TransferEntityOwnershipCommandHandler> logger)
    {
        _loreEntityRepository = loreEntityRepository;
        _campaignMemberRepository = campaignMemberRepository;
        _currentUserService = currentUserService;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    /// <summary>
    /// Handles the transfer entity ownership command.
    /// </summary>
    /// <param name="request">The transfer ownership command.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The updated entity as a DTO.</returns>
    /// <exception cref="ForbiddenAccessException">Thrown when user is not the campaign master.</exception>
    /// <exception cref="NotFoundException">Thrown when entity, campaign, or new owner not found.</exception>
    public async Task<LoreEntityDto> Handle(TransferEntityOwnershipCommand request, CancellationToken cancellationToken)
    {
        if (!_currentUserService.UserId.HasValue)
        {
            throw new ForbiddenAccessException("User must be authenticated to transfer entity ownership");
        }

        var userId = _currentUserService.UserId.Value;

        // Check if current user is a campaign member
        var currentUserMembership = await _campaignMemberRepository
            .GetByCampaignAndUserAsync(request.CampaignId, userId, cancellationToken);
        
        if (currentUserMembership == null)
        {
            throw new NotFoundException("Campaign", request.CampaignId);
        }

        // Only campaign master can transfer ownership
        if (!currentUserMembership.IsMaster)
        {
            throw new ForbiddenAccessException("Only the campaign master can transfer entity ownership");
        }

        // Get the entity
        var entity = await _loreEntityRepository.GetByIdAsync(request.EntityId, cancellationToken);
        
        if (entity == null || entity.CampaignId != request.CampaignId)
        {
            throw new NotFoundException("LoreEntity", request.EntityId);
        }

        // Verify the new owner is a campaign member
        var newOwnerMembership = await _campaignMemberRepository
            .GetByCampaignAndUserAsync(request.CampaignId, request.NewOwnerId, cancellationToken);
        
        if (newOwnerMembership == null)
        {
            throw new NotFoundException("CampaignMember", request.NewOwnerId);
        }

        // Determine ownership type if not specified
        var newOwnershipType = request.NewOwnershipType;
        if (!newOwnershipType.HasValue)
        {
            // Default to Player if new owner is a player, Master if new owner is a master
            newOwnershipType = newOwnerMembership.IsMaster 
                ? OwnershipType.Master 
                : OwnershipType.Player;
        }

        // Transfer ownership
        var previousOwnerId = entity.OwnerId;
        entity.TransferOwnership(request.NewOwnerId, newOwnershipType);
        
        _loreEntityRepository.Update(entity);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "LoreEntity {EntityId} ownership transferred from {PreviousOwnerId} to {NewOwnerId} " +
            "with ownership type {OwnershipType} by master {MasterId}",
            request.EntityId, previousOwnerId, request.NewOwnerId, newOwnershipType, userId);

        return LoreEntityDto.FromEntity(entity);
    }
}
