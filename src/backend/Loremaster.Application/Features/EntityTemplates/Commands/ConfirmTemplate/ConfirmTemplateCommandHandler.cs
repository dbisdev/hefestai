using Loremaster.Application.Common.Interfaces;
using Loremaster.Domain.Entities;
using Loremaster.Domain.Exceptions;
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
    private readonly IGameSystemAuthorizationService _authorizationService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<ConfirmTemplateCommandHandler> _logger;

    public ConfirmTemplateCommandHandler(
        IEntityTemplateRepository templateRepository,
        IGameSystemAuthorizationService authorizationService,
        IUnitOfWork unitOfWork,
        ILogger<ConfirmTemplateCommandHandler> logger)
    {
        _templateRepository = templateRepository;
        _authorizationService = authorizationService;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<ConfirmTemplateResult> Handle(
        ConfirmTemplateCommand request, 
        CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Confirming template {TemplateId} by user {UserId} (IsAdmin: {IsAdmin})",
            request.TemplateId, request.OwnerId, request.IsAdmin);

        var template = await _templateRepository.GetByIdAsync(
            request.TemplateId, cancellationToken);

        if (template == null)
        {
            throw new NotFoundException(nameof(EntityTemplate), request.TemplateId);
        }

        // Delegates ownership check to the shared authorization service.
        // Admins bypass; non-admins must be the system owner.
        await _authorizationService.RequireOwnerOrAdminAsync(
            request.GameSystemId,
            request.OwnerId,
            isAdmin: request.IsAdmin,
            action: "confirm templates",
            cancellationToken);

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
