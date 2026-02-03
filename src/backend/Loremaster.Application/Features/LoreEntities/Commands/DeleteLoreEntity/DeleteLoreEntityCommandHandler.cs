using Loremaster.Application.Common.Interfaces;
using Loremaster.Domain.Exceptions;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Loremaster.Application.Features.LoreEntities.Commands.DeleteLoreEntity;

/// <summary>
/// Handler for DeleteLoreEntityCommand. Performs soft-delete on an entity.
/// Only users with write permissions can delete entities.
/// </summary>
public class DeleteLoreEntityCommandHandler : IRequestHandler<DeleteLoreEntityCommand, Unit>
{
    private readonly ILoreEntityRepository _loreEntityRepository;
    private readonly ICampaignMemberRepository _campaignMemberRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<DeleteLoreEntityCommandHandler> _logger;

    public DeleteLoreEntityCommandHandler(
        ILoreEntityRepository loreEntityRepository,
        ICampaignMemberRepository campaignMemberRepository,
        ICurrentUserService currentUserService,
        IUnitOfWork unitOfWork,
        ILogger<DeleteLoreEntityCommandHandler> logger)
    {
        _loreEntityRepository = loreEntityRepository;
        _campaignMemberRepository = campaignMemberRepository;
        _currentUserService = currentUserService;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    /// <summary>
    /// Handles the delete lore entity command.
    /// </summary>
    /// <param name="request">The delete entity command.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Unit value indicating completion.</returns>
    /// <exception cref="ForbiddenAccessException">Thrown when user lacks write permissions.</exception>
    /// <exception cref="NotFoundException">Thrown when entity or campaign not found.</exception>
    public async Task<Unit> Handle(DeleteLoreEntityCommand request, CancellationToken cancellationToken)
    {
        if (!_currentUserService.UserId.HasValue)
        {
            throw new ForbiddenAccessException("User must be authenticated to delete entities");
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
            throw new ForbiddenAccessException("You don't have permission to delete this entity");
        }

        // Soft delete
        _loreEntityRepository.Delete(entity);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("LoreEntity {EntityId} deleted by user {UserId}", request.EntityId, userId);

        return Unit.Value;
    }
}
