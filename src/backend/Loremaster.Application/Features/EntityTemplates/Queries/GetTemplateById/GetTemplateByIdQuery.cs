using Loremaster.Application.Features.EntityTemplates.DTOs;
using MediatR;

namespace Loremaster.Application.Features.EntityTemplates.Queries.GetTemplateById;

/// <summary>
/// Query to retrieve a single entity template by ID.
/// </summary>
/// <param name="TemplateId">The template ID.</param>
/// <param name="OwnerId">The owner ID for authorization.</param>
public record GetTemplateByIdQuery(
    Guid TemplateId,
    Guid OwnerId) : IRequest<EntityTemplateDto?>;
