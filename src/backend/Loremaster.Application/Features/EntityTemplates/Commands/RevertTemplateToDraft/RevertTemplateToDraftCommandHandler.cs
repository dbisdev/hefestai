using Loremaster.Application.Common.Interfaces;
using Loremaster.Domain.Entities;
using Loremaster.Domain.Exceptions;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Loremaster.Application.Features.EntityTemplates.Commands.RevertTemplateToDraft;

/// <summary>
/// Handler for RevertTemplateToDraftCommand.
/// Reverts a confirmed template back to draft status for editing.
/// </summary>
public class RevertTemplateToDraftCommandHandler : IRequestHandler<RevertTemplateToDraftCommand, Unit>
{
    private readonly IEntityTemplateRepository _templateRepository;
    private readonly IGameSystemAuthorizationService _authorizationService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<RevertTemplateToDraftCommandHandler> _logger;

    public RevertTemplateToDraftCommandHandler(
        IEntityTemplateRepository templateRepository,
        IGameSystemAuthorizationService authorizationService,
        IUnitOfWork unitOfWork,
        ILogger<RevertTemplateToDraftCommandHandler> logger)
    {
        _templateRepository = templateRepository;
        _authorizationService = authorizationService;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Unit> Handle(
        RevertTemplateToDraftCommand request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Reverting template {TemplateId} to draft by user {UserId} (IsAdmin: {IsAdmin})",
            request.TemplateId, request.OwnerId, request.IsAdmin);

        var template = await _templateRepository.GetByIdAsync(
            request.TemplateId, cancellationToken);

        if (template == null)
        {
            throw new NotFoundException(nameof(EntityTemplate), request.TemplateId);
        }

        // Delegates ownership check to the shared authorization service.
        // Admins may revert any template; non-admins must be the system owner.
        await _authorizationService.RequireOwnerOrAdminAsync(
            request.GameSystemId,
            request.OwnerId,
            request.IsAdmin,
            action: "revert templates",
            cancellationToken);

        template.RevertToDraft(adminOverride: request.IsAdmin);

        _templateRepository.Update(template);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Template {TemplateId} reverted to draft status",
            template.Id);

        return Unit.Value;
    }
}
