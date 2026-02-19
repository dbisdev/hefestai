using Loremaster.Application.Common.Interfaces;
using Loremaster.Application.Features.EntityTemplates.DTOs;
using Loremaster.Domain.Entities;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Loremaster.Application.Features.EntityTemplates.Queries.GetTemplateById;

/// <summary>
/// Handler for GetTemplateByIdQuery.
/// Retrieves a single template with full details.
/// </summary>
public class GetTemplateByIdQueryHandler : IRequestHandler<GetTemplateByIdQuery, EntityTemplateDto?>
{
    private readonly IEntityTemplateRepository _templateRepository;
    private readonly ILogger<GetTemplateByIdQueryHandler> _logger;

    public GetTemplateByIdQueryHandler(
        IEntityTemplateRepository templateRepository,
        ILogger<GetTemplateByIdQueryHandler> logger)
    {
        _templateRepository = templateRepository;
        _logger = logger;
    }

    public async Task<EntityTemplateDto?> Handle(
        GetTemplateByIdQuery request, 
        CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Getting template {TemplateId} for owner {OwnerId} in game system {GameSystemId} (IsAdmin: {IsAdmin})",
            request.TemplateId, request.OwnerId, request.GameSystemId, request.IsAdmin);

        // First try to get template owned by the requesting user
        var template = await _templateRepository.GetByIdAsync(
            request.TemplateId, request.OwnerId, cancellationToken);

        // If not found, check if user is Admin or if it's a confirmed template
        if (template == null)
        {
            var anyTemplate = await _templateRepository.GetByIdAsync(
                request.TemplateId, cancellationToken);
            
            if (anyTemplate != null)
            {
                // Admin can view any template, or user can view confirmed templates
                if (request.IsAdmin || anyTemplate.Status == Domain.Enums.TemplateStatus.Confirmed)
                {
                    template = anyTemplate;
                    _logger.LogInformation(
                        "Template {TemplateId} found as {Status} template (IsAdmin: {IsAdmin})",
                        request.TemplateId, anyTemplate.Status, request.IsAdmin);
                }
            }
        }

        if (template == null)
        {
            _logger.LogWarning("Template {TemplateId} not found", request.TemplateId);
            return null;
        }

        return MapToDto(template);
    }

    private static EntityTemplateDto MapToDto(EntityTemplate template)
    {
        var fields = template.GetFieldDefinitions().Select(f => new FieldDefinitionDto(
            f.Name,
            f.DisplayName,
            f.FieldType,
            f.IsRequired,
            f.DefaultValue,
            f.Description,
            f.Order,
            f.GetOptions().ToList(),
            f.MinValue,
            f.MaxValue,
            f.ValidationPattern
        )).ToList();

        return new EntityTemplateDto(
            template.Id,
            template.EntityTypeName,
            template.DisplayName,
            template.Description,
            template.Status,
            fields,
            template.IconHint,
            template.Version,
            template.ReviewNotes,
            template.ConfirmedAt,
            template.ConfirmedByUserId,
            template.GameSystemId,
            template.GameSystem?.Name ?? "",
            template.SourceDocumentId,
            template.OwnerId,
            template.CreatedAt,
            template.UpdatedAt);
    }
}
