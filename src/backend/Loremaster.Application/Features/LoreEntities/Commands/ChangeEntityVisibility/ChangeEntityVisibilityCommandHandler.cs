using Loremaster.Application.Common.Interfaces;
using Loremaster.Application.Features.LoreEntities.DTOs;
using Loremaster.Domain.Exceptions;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Loremaster.Application.Features.LoreEntities.Commands.ChangeEntityVisibility;

/// <summary>
/// Handler for ChangeEntityVisibilityCommand. Changes visibility level of an entity.
/// Only users with write permissions can change visibility.
/// </summary>
public class ChangeEntityVisibilityCommandHandler : IRequestHandler<ChangeEntityVisibilityCommand, LoreEntityDto>
{
    private readonly ILoreEntityRepository _loreEntityRepository;
    private readonly ICampaignMemberRepository _campaignMemberRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<ChangeEntityVisibilityCommandHandler> _logger;

    public ChangeEntityVisibilityCommandHandler(
        ILoreEntityRepository loreEntityRepository,
        ICampaignMemberRepository campaignMemberRepository,
        ICurrentUserService currentUserService,
        IUnitOfWork unitOfWork,
        ILogger<ChangeEntityVisibilityCommandHandler> logger)
    {
        _loreEntityRepository = loreEntityRepository;
        _campaignMemberRepository = campaignMemberRepository;
        _currentUserService = currentUserService;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    /// <summary>
    /// Handles the change entity visibility command.
    /// </summary>
    /// <param name="request">The change visibility command.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The updated entity as a DTO.</returns>
    /// <exception cref="ForbiddenAccessException">Thrown when user lacks write permissions.</exception>
    /// <exception cref="NotFoundException">Thrown when entity or campaign not found.</exception>
    public async Task<LoreEntityDto> Handle(ChangeEntityVisibilityCommand request, CancellationToken cancellationToken)
    {
        if (!_currentUserService.UserId.HasValue)
        {
            throw new ForbiddenAccessException("User must be authenticated to change entity visibility");
        }

        var userId = _currentUserService.UserId.Value;

        // Check campaign membership
        var membership = await _campaignMemberRepository
            .GetByCampaignAndUserAsync(request.CampaignId, userId, cancellationToken);
        
        if (membership == null)
        {
            throw new NotFoundException("Campaign", request.CampaignId);
        }

        // Get the entity
        var entity = await _loreEntityRepository.GetByIdAsync(request.EntityId, cancellationToken);
        
        if (entity == null || entity.CampaignId != request.CampaignId)
        {
            throw new NotFoundException("LoreEntity", request.EntityId);
        }

        // Check write permission
        if (!entity.CanBeWrittenBy(userId, membership.IsMaster))
        {
            throw new ForbiddenAccessException("You don't have permission to change visibility");
        }

        // Change visibility
        entity.ChangeVisibility(request.Visibility);
        _loreEntityRepository.Update(entity);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "LoreEntity {EntityId} visibility changed to {Visibility} by user {UserId}", 
            request.EntityId, request.Visibility, userId);

        return LoreEntityDto.FromEntity(entity);
    }
}
