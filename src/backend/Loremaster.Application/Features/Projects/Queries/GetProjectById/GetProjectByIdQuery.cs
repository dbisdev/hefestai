using Loremaster.Application.Features.Projects.DTOs;
using MediatR;

namespace Loremaster.Application.Features.Projects.Queries.GetProjectById;

public record GetProjectByIdQuery(Guid ProjectId) : IRequest<ProjectDto>;
