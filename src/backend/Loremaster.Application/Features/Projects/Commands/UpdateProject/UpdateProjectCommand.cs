using Loremaster.Application.Features.Projects.DTOs;
using MediatR;

namespace Loremaster.Application.Features.Projects.Commands.UpdateProject;

public record UpdateProjectCommand(
    Guid ProjectId,
    string Name,
    string? Description
) : IRequest<ProjectDto>;
