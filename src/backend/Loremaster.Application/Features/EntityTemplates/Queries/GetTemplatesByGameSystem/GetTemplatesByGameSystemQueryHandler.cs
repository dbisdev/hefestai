using Loremaster.Application.Common.Interfaces;
using Loremaster.Application.Features.EntityTemplates.DTOs;
using Loremaster.Domain.Enums;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Loremaster.Application.Features.EntityTemplates.Queries.GetTemplatesByGameSystem;

/// <summary>
/// Handler for GetTemplatesByGameSystemQuery.
/// Retrieves templates filtered by game system and optionally by status.
/// </summary>
public class GetTemplatesByGameSystemQueryHandler 
    : IRequestHandler<GetTemplatesByGameSystemQuery, GetTemplatesByGameSystemResult>
{
    private readonly IEntityTemplateRepository _templateRepository;
    private readonly ILogger<GetTemplatesByGameSystemQueryHandler> _logger;

    public GetTemplatesByGameSystemQueryHandler(
        IEntityTemplateRepository templateRepository,
        ILogger<GetTemplatesByGameSystemQueryHandler> logger)
    {
        _templateRepository = templateRepository;
        _logger = logger;
    }

    public async Task<GetTemplatesByGameSystemResult> Handle(
        GetTemplatesByGameSystemQuery request, 
        CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Getting templates for game system {GameSystemId} by owner {OwnerId}",
            request.GameSystemId, request.OwnerId);

        var templates = request switch
        {
            // ConfirmedOnly: Get ALL confirmed templates for the game system (regardless of owner)
            // This allows any Master using a campaign with this game system to see available templates
            { ConfirmedOnly: true } => await _templateRepository.GetAllConfirmedByGameSystemIdAsync(
                request.GameSystemId, cancellationToken),
            
            { Status: not null } => await _templateRepository.GetByStatusAsync(
                request.GameSystemId, request.OwnerId, request.Status.Value, cancellationToken),
            
            _ => await _templateRepository.GetByGameSystemIdAsync(
                request.GameSystemId, request.OwnerId, cancellationToken)
        };

        var summaries = templates.Select(t => new EntityTemplateSummaryDto(
            t.Id,
            t.EntityTypeName,
            t.DisplayName,
            t.Status,
            t.GetFieldDefinitions().Count,
            t.IconHint,
            t.CreatedAt
        )).ToList();

        var confirmedCount = templates.Count(t => t.Status == TemplateStatus.Confirmed);
        var pendingCount = templates.Count(t => t.Status == TemplateStatus.PendingReview);

        _logger.LogInformation(
            "Found {Count} templates ({Confirmed} confirmed, {Pending} pending)",
            summaries.Count, confirmedCount, pendingCount);

        return new GetTemplatesByGameSystemResult(
            summaries,
            summaries.Count,
            confirmedCount,
            pendingCount);
    }
}
