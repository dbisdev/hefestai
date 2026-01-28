using Loremaster.Application.Common.Interfaces;
using Loremaster.Application.Features.Projects.DTOs;
using Loremaster.Domain.Exceptions;
using MediatR;

namespace Loremaster.Application.Features.Projects.Commands.UpdateProject;

public class UpdateProjectCommandHandler : IRequestHandler<UpdateProjectCommand, ProjectDto>
{
    private readonly IProjectRepository _projectRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateProjectCommandHandler(
        IProjectRepository projectRepository,
        ICurrentUserService currentUserService,
        IUnitOfWork unitOfWork)
    {
        _projectRepository = projectRepository;
        _currentUserService = currentUserService;
        _unitOfWork = unitOfWork;
    }

    public async Task<ProjectDto> Handle(UpdateProjectCommand request, CancellationToken cancellationToken)
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
            throw new ForbiddenAccessException("You do not have permission to update this project");
        }

        // Check for duplicate name (excluding current project)
        var nameExists = await _projectRepository.NameExistsForOwnerAsync(
            userId, request.Name, request.ProjectId, cancellationToken);
        
        if (nameExists)
        {
            throw new DomainException("You already have another project with this name");
        }

        project.UpdateDetails(request.Name, request.Description);

        await _projectRepository.UpdateAsync(project, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return ProjectDto.FromEntity(project);
    }
}
