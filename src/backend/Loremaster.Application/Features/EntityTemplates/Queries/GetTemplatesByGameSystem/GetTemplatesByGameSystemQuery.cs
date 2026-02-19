using Loremaster.Application.Features.EntityTemplates.DTOs;
using Loremaster.Domain.Enums;
using MediatR;

namespace Loremaster.Application.Features.EntityTemplates.Queries.GetTemplatesByGameSystem;

/// <summary>
/// Query to retrieve all entity templates for a game system.
/// Can optionally filter by status.
/// </summary>
/// <param name="GameSystemId">The game system ID.</param>
/// <param name="OwnerId">The owner ID for authorization.</param>
/// <param name="Status">Optional status filter.</param>
/// <param name="ConfirmedOnly">If true, only return confirmed templates (regardless of owner).</param>
/// <param name="IncludeAll">If true, return all templates regardless of owner and status (for system owners).</param>
public record GetTemplatesByGameSystemQuery(
    Guid GameSystemId,
    Guid OwnerId,
    TemplateStatus? Status = null,
    bool ConfirmedOnly = false,
    bool IncludeAll = false) : IRequest<GetTemplatesByGameSystemResult>;

/// <summary>
/// Result containing list of templates.
/// </summary>
public record GetTemplatesByGameSystemResult(
    IReadOnlyList<EntityTemplateSummaryDto> Templates,
    int TotalCount,
    int ConfirmedCount,
    int PendingCount);
