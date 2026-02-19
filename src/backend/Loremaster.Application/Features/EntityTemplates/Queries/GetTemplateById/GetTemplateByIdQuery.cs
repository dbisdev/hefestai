using Loremaster.Application.Features.EntityTemplates.DTOs;
using MediatR;

namespace Loremaster.Application.Features.EntityTemplates.Queries.GetTemplateById;

/// <summary>
/// Query to retrieve a single entity template by ID.
/// </summary>
/// <param name="TemplateId">The template ID.</param>
/// <param name="GameSystemId">The game system ID for authorization.</param>
/// <param name="OwnerId">The owner ID for authorization.</param>
/// <param name="IsAdmin">Whether the user is an Admin (can view any template).</param>
public record GetTemplateByIdQuery(
    Guid TemplateId,
    Guid GameSystemId,
    Guid OwnerId,
    bool IsAdmin = false) : IRequest<EntityTemplateDto?>;
