using Loremaster.Application.Common.Interfaces;
using Loremaster.Domain.Entities;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Loremaster.Application.Features.EntityTemplates.Commands.DeleteTemplate;

/// <summary>
/// Handler for DeleteTemplateCommand.
/// Deletes a template if no entities are using it, or force deletes if requested.
/// </summary>
public class DeleteTemplateCommandHandler : IRequestHandler<DeleteTemplateCommand, DeleteTemplateResult>
{
    private readonly IEntityTemplateRepository _templateRepository;
    private readonly IGameSystemRepository _gameSystemRepository;
    private readonly ILoreEntityRepository _entityRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<DeleteTemplateCommandHandler> _logger;

    public DeleteTemplateCommandHandler(
        IEntityTemplateRepository templateRepository,
        IGameSystemRepository gameSystemRepository,
        ILoreEntityRepository entityRepository,
        IUnitOfWork unitOfWork,
        ILogger<DeleteTemplateCommandHandler> logger)
    {
        _templateRepository = templateRepository;
        _gameSystemRepository = gameSystemRepository;
        _entityRepository = entityRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    /// <summary>
    /// Handles the delete template command.
    /// </summary>
    /// <param name="request">The delete template request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The result containing deletion details.</returns>
    /// <exception cref="ArgumentException">Thrown when template not found.</exception>
    /// <exception cref="UnauthorizedAccessException">Thrown when user doesn't own the template.</exception>
    /// <exception cref="InvalidOperationException">Thrown when template is in use and force is not enabled.</exception>
    public async Task<DeleteTemplateResult> Handle(
        DeleteTemplateCommand request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Attempting to delete template {TemplateId} by user {UserId}, force={ForceDelete}",
            request.TemplateId, request.OwnerId, request.ForceDelete);

        // Get the template
        var template = await _templateRepository.GetByIdAsync(request.TemplateId, cancellationToken);
        
        if (template == null)
        {
            throw new ArgumentException($"Template with ID {request.TemplateId} not found");
        }

        // Verify game system ownership
        var gameSystem = await _gameSystemRepository.GetByIdAsync(
            request.GameSystemId, cancellationToken);
        
        if (gameSystem == null)
        {
            throw new ArgumentException($"Game system with ID {request.GameSystemId} not found");
        }

        // Check if user is Admin or owner of the game system
        var isSystemOwner = gameSystem.OwnerId == request.OwnerId;
        if (!isSystemOwner)
        {
            throw new UnauthorizedAccessException(
                "You do not have permission to delete templates in this game system. " +
                "Only the system owner can delete templates.");
        }

        // Check if any entities are using this template
        var affectedEntityCount = await _entityRepository.CountByEntityTypeAsync(
            template.GameSystemId,
            template.EntityTypeName,
            cancellationToken);

        if (affectedEntityCount > 0 && !request.ForceDelete)
        {
            throw new InvalidOperationException(
                $"Cannot delete template '{template.DisplayName}' because {affectedEntityCount} " +
                $"entities are using it. Use force delete to proceed anyway.");
        }

        var entityTypeName = template.EntityTypeName;
        
        // Delete the template
        _templateRepository.Delete(template);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Deleted template {TemplateId} '{EntityTypeName}'. Affected entities: {AffectedCount}, Forced: {WasForced}",
            request.TemplateId, entityTypeName, affectedEntityCount, request.ForceDelete && affectedEntityCount > 0);

        return new DeleteTemplateResult(
            request.TemplateId,
            entityTypeName,
            request.ForceDelete && affectedEntityCount > 0,
            affectedEntityCount);
    }
}
