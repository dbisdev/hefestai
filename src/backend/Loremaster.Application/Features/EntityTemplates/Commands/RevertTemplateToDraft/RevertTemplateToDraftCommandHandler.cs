using Loremaster.Application.Common.Interfaces;
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
    private readonly IGameSystemRepository _gameSystemRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<RevertTemplateToDraftCommandHandler> _logger;

    public RevertTemplateToDraftCommandHandler(
        IEntityTemplateRepository templateRepository,
        IGameSystemRepository gameSystemRepository,
        IUnitOfWork unitOfWork,
        ILogger<RevertTemplateToDraftCommandHandler> logger)
    {
        _templateRepository = templateRepository;
        _gameSystemRepository = gameSystemRepository;
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
            throw new ArgumentException($"Template with ID {request.TemplateId} not found");
        }

        var gameSystem = await _gameSystemRepository.GetByIdAsync(
            request.GameSystemId, cancellationToken);

        if (gameSystem == null)
        {
            throw new ArgumentException($"Game system with ID {request.GameSystemId} not found");
        }

        var isSystemOwner = gameSystem.OwnerId == request.OwnerId;
        if (!request.IsAdmin && !isSystemOwner)
        {
            throw new UnauthorizedAccessException(
                "You do not have permission to revert templates in this game system. " +
                "Only the system owner or an Admin can revert templates.");
        }

        template.RevertToDraft(adminOverride: request.IsAdmin);

        _templateRepository.Update(template);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Template {TemplateId} reverted to draft status",
            template.Id);

        return Unit.Value;
    }
}
