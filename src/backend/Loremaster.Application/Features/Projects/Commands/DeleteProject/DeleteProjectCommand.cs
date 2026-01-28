using MediatR;

namespace Loremaster.Application.Features.Projects.Commands.DeleteProject;

public record DeleteProjectCommand(Guid ProjectId) : IRequest<Unit>;
