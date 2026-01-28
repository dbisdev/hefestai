using Loremaster.Application.Common.Interfaces;
using Loremaster.Domain.Exceptions;
using MediatR;

namespace Loremaster.Application.Features.Projects.Commands.ArchiveProject;

public class ArchiveProjectCommandHandler : IRequestHandler<ArchiveProjectCommand, Unit>
{
    private readonly IProjectRepository _projectRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly IUnitOfWork _unitOfWork;

    public ArchiveProjectCommandHandler(
        IProjectRepository projectRepository,
        ICurrentUserService currentUserService,
        IUnitOfWork unitOfWork)
    {
        _projectRepository = projectRepository;
        _currentUserService = currentUserService;
        _unitOfWork = unitOfWork;
    }

    public async Task<Unit> Handle(ArchiveProjectCommand request, CancellationToken cancellationToken)
    {
        if (!_currentUserService.UserId.HasValue)
        {
            throw new ForbiddenAccessException("User must be authenticated");
        }

        var userId = _currentUserService.UserId.Value;

        var project = await _projectRepository.GetByIdAsync(request.ProjectId, cancellationToken);
        
        if (project == null)
        {
            throw new NotFoundException("Project", request.ProjectId);
        }

        if (!project.IsOwnedBy(userId))
        {
            throw new ForbiddenAccessException("You do not have permission to modify this project");
        }

        if (request.Archive)
        {
            project.Archive();
        }
        else
        {
            project.Restore();
        }

        await _projectRepository.UpdateAsync(project, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Unit.Value;
    }
}
