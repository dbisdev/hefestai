using Loremaster.Application.Common.Interfaces;
using Loremaster.Application.Features.GameSystems.Commands.CreateGameSystem;
using Loremaster.Domain.Entities;
using Loremaster.Domain.Exceptions;
using Microsoft.Extensions.Logging;

namespace Loremaster.Tests.Unit.Application.Features.GameSystems;

/// <summary>
/// Unit tests for CreateGameSystemCommandHandler.
/// Tests game system creation and duplicate code validation.
/// </summary>
public class CreateGameSystemCommandHandlerTests
{
    private readonly Mock<IGameSystemRepository> _gameSystemRepositoryMock;
    private readonly Mock<ICurrentUserService> _currentUserServiceMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<ILogger<CreateGameSystemCommandHandler>> _loggerMock;
    private readonly CreateGameSystemCommandHandler _handler;

    private readonly Guid _adminUserId = Guid.NewGuid();

    public CreateGameSystemCommandHandlerTests()
    {
        _gameSystemRepositoryMock = new Mock<IGameSystemRepository>();
        _currentUserServiceMock = new Mock<ICurrentUserService>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _loggerMock = new Mock<ILogger<CreateGameSystemCommandHandler>>();

        _handler = new CreateGameSystemCommandHandler(
            _gameSystemRepositoryMock.Object,
            _currentUserServiceMock.Object,
            _unitOfWorkMock.Object,
            _loggerMock.Object);

        // Default setup - authenticated admin user
        _currentUserServiceMock.Setup(x => x.UserId).Returns(_adminUserId);
    }

    #region Success Cases

    [Fact]
    public async Task Handle_WithValidData_ShouldCreateGameSystem()
    {
        // Arrange
        SetupCodeNotExists("dnd5e");

        var command = new CreateGameSystemCommand(
            Code: "dnd5e",
            Name: "Dungeons & Dragons 5th Edition",
            Publisher: "Wizards of the Coast",
            Version: "5.2",
            Description: "The premier fantasy TTRPG");

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Code.Should().Be("dnd5e");
        result.Name.Should().Be("Dungeons & Dragons 5th Edition");
        result.Publisher.Should().Be("Wizards of the Coast");
        result.Version.Should().Be("5.2");
        result.Description.Should().Be("The premier fantasy TTRPG");
        result.IsActive.Should().BeTrue();

        _gameSystemRepositoryMock.Verify(
            x => x.AddAsync(It.IsAny<GameSystem>(), It.IsAny<CancellationToken>()), 
            Times.Once);
        _unitOfWorkMock.Verify(
            x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), 
            Times.Once);
    }

    [Fact]
    public async Task Handle_WithMinimalData_ShouldCreateGameSystem()
    {
        // Arrange
        SetupCodeNotExists("pf2e");

        var command = new CreateGameSystemCommand(
            Code: "pf2e",
            Name: "Pathfinder 2nd Edition");

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Code.Should().Be("pf2e");
        result.Name.Should().Be("Pathfinder 2nd Edition");
        result.Publisher.Should().BeNull();
        result.Version.Should().BeNull();
        result.Description.Should().BeNull();
    }

    [Fact]
    public async Task Handle_WithSupportedEntityTypes_ShouldStoreTypes()
    {
        // Arrange
        SetupCodeNotExists("custom");

        var entityTypes = new List<string> { "character", "monster", "spell" };
        var command = new CreateGameSystemCommand(
            Code: "custom",
            Name: "Custom System",
            SupportedEntityTypes: entityTypes);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.SupportedEntityTypes.Should().NotBeNull();
        result.SupportedEntityTypes.Should().Contain(entityTypes);
    }

    #endregion

    #region Failure Cases

    [Fact]
    public async Task Handle_WhenCodeAlreadyExists_ShouldThrowDomainException()
    {
        // Arrange
        _gameSystemRepositoryMock
            .Setup(x => x.ExistsByCodeAsync("existing", It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var command = new CreateGameSystemCommand(
            Code: "existing",
            Name: "Duplicate System");

        // Act
        var act = () => _handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<DomainException>()
            .WithMessage("*already exists*");
    }

    #endregion

    #region Helper Methods

    private void SetupCodeNotExists(string code)
    {
        _gameSystemRepositoryMock
            .Setup(x => x.ExistsByCodeAsync(code, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
    }

    #endregion
}
