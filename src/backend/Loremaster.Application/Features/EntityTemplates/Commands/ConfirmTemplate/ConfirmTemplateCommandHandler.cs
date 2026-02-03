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
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<ConfirmTemplateCommandHandler> _logger;

    public ConfirmTemplateCommandHandler(
        IEntityTemplateRepository templateRepository,
        IUnitOfWork unitOfWork,
        ILogger<ConfirmTemplateCommandHandler> logger)
    {
        _templateRepository = templateRepository;
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

        if (!template.IsOwnedBy(request.OwnerId))
        {
            throw new UnauthorizedAccessException("You do not have permission to confirm this template");
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
