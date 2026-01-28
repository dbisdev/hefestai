using Loremaster.Application.Common.Interfaces;
using Loremaster.Application.Features.Projects.DTOs;
using Loremaster.Domain.Exceptions;
using MediatR;

namespace Loremaster.Application.Features.Projects.Queries.GetProjects;

public class GetProjectsQueryHandler : IRequestHandler<GetProjectsQuery, IReadOnlyList<ProjectListDto>>
{
    private readonly IProjectRepository _projectRepository;
    private readonly ICurrentUserService _currentUserService;

    public GetProjectsQueryHandler(
        IProjectRepository projectRepository,
        ICurrentUserService currentUserService)
    {
        _projectRepository = projectRepository;
        _currentUserService = currentUserService;
    }

    public async Task<IReadOnlyList<ProjectListDto>> Handle(GetProjectsQuery request, CancellationToken cancellationToken)
    {
        if (!_currentUserService.UserId.HasValue)
        {
            throw new ForbiddenAccessException("User must be authenticated");
        }

        var userId = _currentUserService.UserId.Value;

        var projects = await _projectRepository.GetByOwnerIdAsync(userId, request.Status, cancellationToken);

        return projects.Select(ProjectListDto.FromEntity).ToList();
    }
}
