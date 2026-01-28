using MediatR;

namespace Loremaster.Application.Features.Projects.Commands.ArchiveProject;

public record ArchiveProjectCommand(Guid ProjectId, bool Archive = true) : IRequest<Unit>;
