using Loremaster.Application.Common.Interfaces;
using Loremaster.Application.Features.Projects.Commands.CreateProject;
using Loremaster.Domain.Entities;
using Loremaster.Domain.Exceptions;

namespace Loremaster.Tests.Unit.Application.Features.Projects;

public class CreateProjectCommandHandlerTests
{
    private readonly Mock<IProjectRepository> _projectRepositoryMock;
    private readonly Mock<ICurrentUserService> _currentUserServiceMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly CreateProjectCommandHandler _handler;
    private readonly Guid _userId = Guid.NewGuid();

    public CreateProjectCommandHandlerTests()
    {
        _projectRepositoryMock = new Mock<IProjectRepository>();
        _currentUserServiceMock = new Mock<ICurrentUserService>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();

        _handler = new CreateProjectCommandHandler(
            _projectRepositoryMock.Object,
            _currentUserServiceMock.Object,
            _unitOfWorkMock.Object);

        // Default setup - authenticated user
        _currentUserServiceMock.Setup(x => x.UserId).Returns(_userId);
    }

    [Fact]
    public async Task Handle_WithValidData_ShouldCreateProject()
    {
        // Arrange
        var command = new CreateProjectCommand("My Project", "Description");
        
        _projectRepositoryMock.Setup(x => x.GetCountByOwnerAsync(_userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);
        _projectRepositoryMock.Setup(x => x.NameExistsForOwnerAsync(_userId, command.Name, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Name.Should().Be(command.Name);
        result.Description.Should().Be(command.Description);
        result.OwnerId.Should().Be(_userId);

        _projectRepositoryMock.Verify(x => x.AddAsync(It.IsAny<Project>(), It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenNotAuthenticated_ShouldThrowForbiddenAccessException()
    {
        // Arrange
        _currentUserServiceMock.Setup(x => x.UserId).Returns((Guid?)null);
        var command = new CreateProjectCommand("My Project", null);

        // Act
        var act = () => _handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ForbiddenAccessException>()
            .WithMessage("*authenticated*");
    }

    [Fact]
    public async Task Handle_WhenProjectLimitReached_ShouldThrowDomainException()
    {
        // Arrange
        var command = new CreateProjectCommand("My Project", null);
        
        _projectRepositoryMock.Setup(x => x.GetCountByOwnerAsync(_userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(50); // Max limit

        // Act
        var act = () => _handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<DomainException>()
            .WithMessage("*maximum limit*");
    }

    [Fact]
    public async Task Handle_WhenDuplicateName_ShouldThrowDomainException()
    {
        // Arrange
        var command = new CreateProjectCommand("Existing Project", null);
        
        _projectRepositoryMock.Setup(x => x.GetCountByOwnerAsync(_userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);
        _projectRepositoryMock.Setup(x => x.NameExistsForOwnerAsync(_userId, command.Name, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var act = () => _handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<DomainException>()
            .WithMessage("*already have a project with this name*");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(25)]
    [InlineData(49)]
    public async Task Handle_WhenUnderProjectLimit_ShouldSucceed(int currentCount)
    {
        // Arrange
        var command = new CreateProjectCommand("New Project", null);
        
        _projectRepositoryMock.Setup(x => x.GetCountByOwnerAsync(_userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(currentCount);
        _projectRepositoryMock.Setup(x => x.NameExistsForOwnerAsync(_userId, command.Name, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
    }
}
