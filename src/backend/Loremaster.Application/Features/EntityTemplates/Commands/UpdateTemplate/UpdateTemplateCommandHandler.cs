using Loremaster.Application.Common.Interfaces;
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
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<UpdateTemplateCommandHandler> _logger;

    public UpdateTemplateCommandHandler(
        IEntityTemplateRepository templateRepository,
        IUnitOfWork unitOfWork,
        ILogger<UpdateTemplateCommandHandler> logger)
    {
        _templateRepository = templateRepository;
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
            request.TemplateId, request.OwnerId, cancellationToken);

        if (template == null)
        {
            throw new ArgumentException($"Template with ID {request.TemplateId} not found");
        }

        // Admin can update any template, others can only update their own
        if (!request.IsAdmin && !template.IsOwnedBy(request.OwnerId))
        {
            throw new UnauthorizedAccessException("You do not have permission to update this template");
        }

        // Update metadata (pass adminOverride flag)
        template.Update(
            request.DisplayName,
            request.Description,
            request.IconHint,
            request.Version,
            adminOverride: request.IsAdmin);

        // Update fields if provided (pass adminOverride flag)
        if (request.Fields != null)
        {
            var fields = request.Fields.Select(f => FieldDefinition.Create(
                f.Name,
                f.DisplayName,
                f.FieldType,
                f.IsRequired,
                f.DefaultValue,
                f.Description,
                f.Order,
                f.Options,
                f.MinValue,
                f.MaxValue,
                f.ValidationPattern
            )).ToList();

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
