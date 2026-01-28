using Loremaster.Application.Common.Interfaces;
using Loremaster.Application.Features.Projects.DTOs;
using Loremaster.Domain.Exceptions;
using MediatR;

namespace Loremaster.Application.Features.Projects.Queries.GetProjectById;

public class GetProjectByIdQueryHandler : IRequestHandler<GetProjectByIdQuery, ProjectDto>
{
    private readonly IProjectRepository _projectRepository;
    private readonly ICurrentUserService _currentUserService;

    public GetProjectByIdQueryHandler(
        IProjectRepository projectRepository,
        ICurrentUserService currentUserService)
    {
        _projectRepository = projectRepository;
        _currentUserService = currentUserService;
    }

    public async Task<ProjectDto> Handle(GetProjectByIdQuery request, CancellationToken cancellationToken)
    {
        if (!_currentUserService.UserId.HasValue)
        {
            throw new ForbiddenAccessException("User must be authenticated");
        }

        var userId = _currentUserService.UserId.Value;

        var project = await _projectRepository.GetByIdWithOwnerAsync(request.ProjectId, cancellationToken);
        
        if (project == null)
        {
            throw new NotFoundException("Project", request.ProjectId);
        }

        if (!project.IsOwnedBy(userId))
        {
            throw new ForbiddenAccessException("You do not have permission to view this project");
        }

        return ProjectDto.FromEntity(project);
    }
}
