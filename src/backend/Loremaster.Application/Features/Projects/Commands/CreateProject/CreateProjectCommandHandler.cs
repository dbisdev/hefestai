using Loremaster.Application.Common.Interfaces;
using Loremaster.Application.Features.Projects.DTOs;
using Loremaster.Domain.Entities;
using Loremaster.Domain.Exceptions;
using MediatR;

namespace Loremaster.Application.Features.Projects.Commands.CreateProject;

public class CreateProjectCommandHandler : IRequestHandler<CreateProjectCommand, ProjectDto>
{
    private readonly IProjectRepository _projectRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly IUnitOfWork _unitOfWork;

    private const int MaxProjectsPerUser = 50;

    public CreateProjectCommandHandler(
        IProjectRepository projectRepository,
        ICurrentUserService currentUserService,
        IUnitOfWork unitOfWork)
    {
        _projectRepository = projectRepository;
        _currentUserService = currentUserService;
        _unitOfWork = unitOfWork;
    }

    public async Task<ProjectDto> Handle(CreateProjectCommand request, CancellationToken cancellationToken)
    {
        if (!_currentUserService.UserId.HasValue)
        {
            throw new ForbiddenAccessException("User must be authenticated to create a project");
        }

        var userId = _currentUserService.UserId.Value;

        // Check if user has reached project limit
        var projectCount = await _projectRepository.GetCountByOwnerAsync(userId, cancellationToken);
        if (projectCount >= MaxProjectsPerUser)
        {
            throw new DomainException($"You have reached the maximum limit of {MaxProjectsPerUser} projects");
        }

        // Check for duplicate project name
        var nameExists = await _projectRepository.NameExistsForOwnerAsync(
            userId, request.Name, cancellationToken: cancellationToken);
        
        if (nameExists)
        {
            throw new DomainException("You already have a project with this name");
        }

        var project = Project.Create(request.Name, userId, request.Description);

        await _projectRepository.AddAsync(project, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return ProjectDto.FromEntity(project);
    }
}
