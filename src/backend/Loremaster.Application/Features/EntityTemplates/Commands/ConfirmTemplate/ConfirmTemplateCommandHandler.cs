using Loremaster.Application.Common.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Loremaster.Application.Features.EntityTemplates.Commands.ConfirmTemplate;

/// <summary>
/// Handler for ConfirmTemplateCommand.
/// Confirms a template, making it available for entity creation.
/// </summary>
public class ConfirmTemplateCommandHandler : IRequestHandler<ConfirmTemplateCommand, ConfirmTemplateResult>
{
    private readonly IEntityTemplateRepository _templateRepository;
    private readonly IGameSystemRepository _gameSystemRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<ConfirmTemplateCommandHandler> _logger;

    public ConfirmTemplateCommandHandler(
        IEntityTemplateRepository templateRepository,
        IGameSystemRepository gameSystemRepository,
        IUnitOfWork unitOfWork,
        ILogger<ConfirmTemplateCommandHandler> logger)
    {
        _templateRepository = templateRepository;
        _gameSystemRepository = gameSystemRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<ConfirmTemplateResult> Handle(
        ConfirmTemplateCommand request, 
        CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Confirming template {TemplateId} by user {UserId}",
            request.TemplateId, request.OwnerId);

        var template = await _templateRepository.GetByIdAsync(
            request.TemplateId, request.OwnerId, cancellationToken);

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
                "You do not have permission to confirm templates in this game system. " +
                "Only the system owner can confirm templates.");
        }

        // Confirm the template
        template.Confirm(request.OwnerId, request.Notes);
        
        _templateRepository.Update(template);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Template {TemplateId} ({EntityType}) confirmed successfully",
            template.Id, template.EntityTypeName);

        return new ConfirmTemplateResult(
            template.Id,
            template.EntityTypeName,
            template.ConfirmedAt!.Value);
    }
}
