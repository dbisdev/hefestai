using Loremaster.Application.Common.Interfaces;
using Loremaster.Domain.Constants;
using Loremaster.Domain.Enums;
using Loremaster.Domain.ValueObjects;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Loremaster.Application.Features.EntityTemplates.Commands.UpdateTemplate;

/// <summary>
/// Handler for UpdateTemplateCommand.
/// Updates template metadata and optionally field definitions.
/// </summary>
public class UpdateTemplateCommandHandler : IRequestHandler<UpdateTemplateCommand, UpdateTemplateResult>
{
    private readonly IEntityTemplateRepository _templateRepository;
    private readonly IGameSystemRepository _gameSystemRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<UpdateTemplateCommandHandler> _logger;

    public UpdateTemplateCommandHandler(
        IEntityTemplateRepository templateRepository,
        IGameSystemRepository gameSystemRepository,
        IUnitOfWork unitOfWork,
        ILogger<UpdateTemplateCommandHandler> logger)
    {
        _templateRepository = templateRepository;
        _gameSystemRepository = gameSystemRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<UpdateTemplateResult> Handle(
        UpdateTemplateCommand request, 
        CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Updating template {TemplateId} by user {UserId} (IsAdmin: {IsAdmin})",
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
                "You do not have permission to update templates in this game system. " +
                "Only the system owner or an Admin can update templates.");
        }

        if (!string.IsNullOrWhiteSpace(request.EntityTypeName))
        {
            if (!CanonicalEntityTypes.IsValid(request.EntityTypeName))
            {
                throw new ArgumentException(
                    $"Invalid entity type '{request.EntityTypeName}'. " +
                    $"Valid types are: {string.Join(", ", CanonicalEntityTypes.All)}");
            }

            template.ChangeEntityTypeName(request.EntityTypeName, adminOverride: request.IsAdmin);
            _logger.LogInformation(
                "Changed entity type of template {TemplateId} to {EntityTypeName}",
                request.TemplateId, request.EntityTypeName);
        }

        template.Update(
            request.DisplayName,
            request.Description,
            request.IconHint,
            request.Version,
            adminOverride: request.IsAdmin);

        if (request.Fields != null)
        {
            var fields = request.Fields.Select(f =>
            {
                var fieldType = f.FieldType;
                var options = f.Options;
                
                if ((fieldType == FieldType.Select || fieldType == FieldType.MultiSelect) 
                    && (options == null || !options.Any()))
                {
                    fieldType = FieldType.Text;
                    options = null;
                }
                
                return FieldDefinition.Create(
                    f.Name,
                    f.DisplayName,
                    fieldType,
                    f.IsRequired,
                    f.DefaultValue,
                    f.Description,
                    f.Order,
                    options,
                    f.MinValue,
                    f.MaxValue,
                    f.ValidationPattern
                );
            }).ToList();

            template.SetFieldDefinitions(fields, adminOverride: request.IsAdmin);
        }

        _templateRepository.Update(template);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Template {TemplateId} updated with {FieldCount} fields",
            template.Id, template.GetFieldDefinitions().Count);

        return new UpdateTemplateResult(
            template.Id,
            template.EntityTypeName,
            template.DisplayName,
            template.GetFieldDefinitions().Count,
            template.UpdatedAt ?? DateTime.UtcNow);
    }
}
