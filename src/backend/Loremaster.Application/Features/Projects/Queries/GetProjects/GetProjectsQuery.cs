using Loremaster.Application.Features.Projects.DTOs;
using Loremaster.Domain.Enums;
using MediatR;

namespace Loremaster.Application.Features.Projects.Queries.GetProjects;

public record GetProjectsQuery(ProjectStatus? Status = null) : IRequest<IReadOnlyList<ProjectListDto>>;
