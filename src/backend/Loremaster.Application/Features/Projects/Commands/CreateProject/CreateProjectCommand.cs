using Loremaster.Application.Features.Projects.DTOs;
using MediatR;

namespace Loremaster.Application.Features.Projects.Commands.CreateProject;

public record CreateProjectCommand(
    string Name,
    string? Description
) : IRequest<ProjectDto>;
