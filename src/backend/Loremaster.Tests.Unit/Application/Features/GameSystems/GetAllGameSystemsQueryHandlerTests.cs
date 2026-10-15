using Loremaster.Application.Common.Interfaces;
using Loremaster.Application.Features.GameSystems.Queries.GetAllGameSystems;
using Loremaster.Domain.Entities;

namespace Loremaster.Tests.Unit.Application.Features.GameSystems;

/// <summary>
/// Unit tests for GetAllGameSystemsQueryHandler.
/// Tests ownership-scoped retrieval of active game systems.
/// </summary>
public class GetAllGameSystemsQueryHandlerTests
{
    private readonly Mock<IGameSystemRepository> _gameSystemRepositoryMock;
    private readonly GetAllGameSystemsQueryHandler _handler;

    public GetAllGameSystemsQueryHandlerTests()
    {
        _gameSystemRepositoryMock = new Mock<IGameSystemRepository>();
        _handler = new GetAllGameSystemsQueryHandler(_gameSystemRepositoryMock.Object);
    }

    [Fact]
    public async Task Handle_WhenActiveSystemsExist_ShouldReturnMappedDtos()
    {
        // Arrange
        var gameSystems = new List<GameSystem>
        {
            CreateGameSystem("dnd5e", "D&D 5th Edition"),
            CreateGameSystem("pf2e", "Pathfinder 2e"),
            CreateGameSystem("swrpg", "Star Wars RPG")
        };

        var masterId = Guid.NewGuid();
        _gameSystemRepositoryMock
            .Setup(x => x.GetActiveForUserAsync(masterId, false, false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(gameSystems);

        var query = new GetAllGameSystemsQuery(UserId: masterId, IncludeAll: false);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(3);
        result.Select(gs => gs.Code).Should().Contain(new[] { "dnd5e", "pf2e", "swrpg" });
    }

    [Fact]
    public async Task Handle_WhenNoActiveSystems_ShouldReturnEmptyCollection()
    {
        // Arrange
        var masterId = Guid.NewGuid();
        _gameSystemRepositoryMock
            .Setup(x => x.GetActiveForUserAsync(masterId, false, false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<GameSystem>());

        var query = new GetAllGameSystemsQuery(UserId: masterId, IncludeAll: false);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_ShouldMapToDto()
    {
        // Arrange
        var gameSystem = CreateGameSystemWithDetails(
            code: "dnd5e",
            name: "D&D 5th Edition",
            publisher: "Wizards of the Coast",
            version: "5.2",
            description: "Fantasy RPG");

        var masterId = Guid.NewGuid();
        _gameSystemRepositoryMock
            .Setup(x => x.GetActiveForUserAsync(masterId, false, false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<GameSystem> { gameSystem });

        var query = new GetAllGameSystemsQuery(UserId: masterId, IncludeAll: false);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        var dto = result.Should().ContainSingle().Subject;
        dto.Code.Should().Be("dnd5e");
        dto.Name.Should().Be("D&D 5th Edition");
        dto.Publisher.Should().Be("Wizards of the Coast");
        dto.Version.Should().Be("5.2");
        dto.Description.Should().Be("Fantasy RPG");
        dto.IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_WhenAdmin_ShouldPassIncludeAllTrue()
    {
        // Arrange
        var allSystems = new List<GameSystem>
        {
            CreateGameSystem("sys1", "System 1"),
            CreateGameSystem("sys2", "System 2")
        };

        _gameSystemRepositoryMock
            .Setup(x => x.GetActiveForUserAsync(null, true, false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(allSystems);

        var query = new GetAllGameSystemsQuery(UserId: null, IncludeAll: true);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().HaveCount(2);
        _gameSystemRepositoryMock.Verify(
            x => x.GetActiveForUserAsync(null, true, false, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WhenPlayer_ShouldPassIsPlayerTrue()
    {
        // Arrange
        var playerId = Guid.NewGuid();
        var visibleSystems = new List<GameSystem>
        {
            CreateGameSystem("admin-sys", "Admin Owned System"),
            CreateGameSystem("master-sys", "Campaign Master System")
        };

        _gameSystemRepositoryMock
            .Setup(x => x.GetActiveForUserAsync(playerId, false, true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(visibleSystems);

        var query = new GetAllGameSystemsQuery(UserId: playerId, IncludeAll: false, IsPlayer: true);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().HaveCount(2);
        _gameSystemRepositoryMock.Verify(
            x => x.GetActiveForUserAsync(playerId, false, true, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    #region Helper Methods

    private static readonly Guid TestOwnerId = Guid.NewGuid();

    private static GameSystem CreateGameSystem(string code, string name)
        => GameSystem.Create(code, name, TestOwnerId);

    private static GameSystem CreateGameSystemWithDetails(
        string code,
        string name,
        string? publisher = null,
        string? version = null,
        string? description = null)
        => GameSystem.Create(code, name, TestOwnerId, publisher, version, description);

    #endregion
}
