using Loremaster.Application.Common.Interfaces;
using Loremaster.Application.Features.Projects.Queries.GetProjects;
using Loremaster.Domain.Entities;
using Loremaster.Domain.Enums;
using Loremaster.Domain.Exceptions;

namespace Loremaster.Tests.Unit.Application.Features.Projects;

public class GetProjectsQueryHandlerTests
{
    private readonly Mock<IProjectRepository> _projectRepositoryMock;
    private readonly Mock<ICurrentUserService> _currentUserServiceMock;
    private readonly GetProjectsQueryHandler _handler;
    private readonly Guid _userId = Guid.NewGuid();

    public GetProjectsQueryHandlerTests()
    {
        _projectRepositoryMock = new Mock<IProjectRepository>();
        _currentUserServiceMock = new Mock<ICurrentUserService>();

        _handler = new GetProjectsQueryHandler(
            _projectRepositoryMock.Object,
            _currentUserServiceMock.Object);

        // Default setup - authenticated user
        _currentUserServiceMock.Setup(x => x.UserId).Returns(_userId);
    }

    [Fact]
    public async Task Handle_WithNoStatusFilter_ShouldReturnAllProjects()
    {
        // Arrange
        var projects = new List<Project>
        {
            Project.Create("Project 1", _userId, "Description 1"),
            Project.Create("Project 2", _userId, "Description 2"),
            Project.Create("Project 3", _userId)
        };

        _projectRepositoryMock.Setup(x => x.GetByOwnerIdAsync(_userId, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(projects);

        var query = new GetProjectsQuery();

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().HaveCount(3);
        result.Should().AllSatisfy(p => p.Status.Should().Be(ProjectStatus.Active));
    }

    [Fact]
    public async Task Handle_WithActiveStatusFilter_ShouldReturnOnlyActiveProjects()
    {
        // Arrange
        var projects = new List<Project>
        {
            Project.Create("Active Project", _userId)
        };

        _projectRepositoryMock.Setup(x => x.GetByOwnerIdAsync(_userId, ProjectStatus.Active, It.IsAny<CancellationToken>()))
            .ReturnsAsync(projects);

        var query = new GetProjectsQuery(ProjectStatus.Active);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().HaveCount(1);
        _projectRepositoryMock.Verify(x => x.GetByOwnerIdAsync(_userId, ProjectStatus.Active, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenNotAuthenticated_ShouldThrowForbiddenAccessException()
    {
        // Arrange
        _currentUserServiceMock.Setup(x => x.UserId).Returns((Guid?)null);
        var query = new GetProjectsQuery();

        // Act
        var act = () => _handler.Handle(query, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ForbiddenAccessException>()
            .WithMessage("*authenticated*");
    }

    [Fact]
    public async Task Handle_WhenUserHasNoProjects_ShouldReturnEmptyList()
    {
        // Arrange
        _projectRepositoryMock.Setup(x => x.GetByOwnerIdAsync(_userId, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Project>());

        var query = new GetProjectsQuery();

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_ShouldReturnProjectListDtos()
    {
        // Arrange
        var project = Project.Create("Test Project", _userId, "Test Description");
        
        _projectRepositoryMock.Setup(x => x.GetByOwnerIdAsync(_userId, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Project> { project });

        var query = new GetProjectsQuery();

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().HaveCount(1);
        var dto = result.First();
        dto.Name.Should().Be("Test Project");
        dto.Description.Should().Be("Test Description");
        dto.Status.Should().Be(ProjectStatus.Active);
    }
}
