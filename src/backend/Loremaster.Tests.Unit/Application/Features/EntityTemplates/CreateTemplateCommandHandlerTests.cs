using Loremaster.Application.Common.Interfaces;
using Loremaster.Application.Features.EntityTemplates.Commands.CreateTemplate;
using Loremaster.Application.Features.EntityTemplates.DTOs;
using Loremaster.Domain.Entities;
using Loremaster.Domain.Enums;
using Loremaster.Domain.ValueObjects;
using Microsoft.Extensions.Logging;

namespace Loremaster.Tests.Unit.Application.Features.EntityTemplates;

/// <summary>
/// Unit tests for CreateTemplateCommandHandler.
/// Tests template creation, game system validation, and duplicate detection.
/// </summary>
public class CreateTemplateCommandHandlerTests
{
    private readonly Mock<IEntityTemplateRepository> _templateRepositoryMock;
    private readonly Mock<IGameSystemRepository> _gameSystemRepositoryMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<ILogger<CreateTemplateCommandHandler>> _loggerMock;
    private readonly CreateTemplateCommandHandler _handler;

    private readonly Guid _ownerId = Guid.NewGuid();
    private readonly Guid _gameSystemId = Guid.NewGuid();

    public CreateTemplateCommandHandlerTests()
    {
        _templateRepositoryMock = new Mock<IEntityTemplateRepository>();
        _gameSystemRepositoryMock = new Mock<IGameSystemRepository>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _loggerMock = new Mock<ILogger<CreateTemplateCommandHandler>>();

        _handler = new CreateTemplateCommandHandler(
            _templateRepositoryMock.Object,
            _gameSystemRepositoryMock.Object,
            _unitOfWorkMock.Object,
            _loggerMock.Object);
    }

    #region Success Cases

    [Fact]
    public async Task Handle_WithValidCommand_ShouldCreateTemplate()
    {
        // Arrange
        SetupGameSystem();
        SetupNoExistingTemplate("character");

        var command = new CreateTemplateCommand(
            GameSystemId: _gameSystemId,
            OwnerId: _ownerId,
            EntityTypeName: "Character",
            DisplayName: "Character Template",
            Description: "Template for player characters");

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.EntityTypeName.Should().Be("character"); // Normalized
        result.DisplayName.Should().Be("Character Template");
        result.FieldCount.Should().Be(0);
        result.TemplateId.Should().NotBeEmpty();

        _templateRepositoryMock.Verify(
            x => x.AddAsync(It.IsAny<EntityTemplate>(), It.IsAny<CancellationToken>()),
            Times.Once);
        _unitOfWorkMock.Verify(
            x => x.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WithFields_ShouldCreateTemplateWithFieldDefinitions()
    {
        // Arrange
        SetupGameSystem();
        SetupNoExistingTemplate("vehicle");

        var fields = new List<FieldDefinitionDto>
        {
            new("name", "Name", FieldType.Text, true, null, null, 1, null, null, null, null),
            new("speed", "Speed", FieldType.Number, false, null, null, 2, null, null, null, null),
            new("description", "Description", FieldType.TextArea, false, null, null, 3, null, null, null, null)
        };

        var command = new CreateTemplateCommand(
            GameSystemId: _gameSystemId,
            OwnerId: _ownerId,
            EntityTypeName: "Vehicle",
            DisplayName: "Vehicle Template",
            Fields: fields);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.FieldCount.Should().Be(3);
        result.EntityTypeName.Should().Be("vehicle");

        _templateRepositoryMock.Verify(
            x => x.AddAsync(
                It.Is<EntityTemplate>(t => t.GetFieldDefinitions().Count == 3),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WithOptionalMetadata_ShouldIncludeAllMetadata()
    {
        // Arrange
        SetupGameSystem();
        SetupNoExistingTemplate("npc");

        var command = new CreateTemplateCommand(
            GameSystemId: _gameSystemId,
            OwnerId: _ownerId,
            EntityTypeName: "NPC",
            DisplayName: "Non-Player Character",
            Description: "Template for NPCs",
            IconHint: "person",
            Version: "1.0");

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.DisplayName.Should().Be("Non-Player Character");
        result.EntityTypeName.Should().Be("npc");
    }

    [Theory]
    [InlineData("Character", "character")]
    [InlineData("Player Character", "player_character")]
    [InlineData("Non-Player Character", "non_player_character")]
    [InlineData("VEHICLE", "vehicle")]
    [InlineData("magic_item", "magic_item")]
    public async Task Handle_ShouldNormalizeEntityTypeName(string input, string expected)
    {
        // Arrange
        SetupGameSystem();
        SetupNoExistingTemplate(expected);

        var command = new CreateTemplateCommand(
            GameSystemId: _gameSystemId,
            OwnerId: _ownerId,
            EntityTypeName: input,
            DisplayName: "Test Template");

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.EntityTypeName.Should().Be(expected);
    }

    #endregion

    #region Validation Failures

    [Fact]
    public async Task Handle_WhenGameSystemNotFound_ShouldThrowArgumentException()
    {
        // Arrange
        _gameSystemRepositoryMock
            .Setup(x => x.GetByIdAsync(_gameSystemId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((GameSystem?)null);

        var command = new CreateTemplateCommand(
            GameSystemId: _gameSystemId,
            OwnerId: _ownerId,
            EntityTypeName: "Character",
            DisplayName: "Character Template");

        // Act
        var act = () => _handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage($"*Game system with ID {_gameSystemId} not found*");

        _templateRepositoryMock.Verify(
            x => x.AddAsync(It.IsAny<EntityTemplate>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_WhenDuplicateTemplateExists_ShouldThrowArgumentException()
    {
        // Arrange
        SetupGameSystem();
        SetupExistingTemplate("character");

        var command = new CreateTemplateCommand(
            GameSystemId: _gameSystemId,
            OwnerId: _ownerId,
            EntityTypeName: "Character",
            DisplayName: "Duplicate Character");

        // Act
        var act = () => _handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*template with entity type*already exists*");

        _templateRepositoryMock.Verify(
            x => x.AddAsync(It.IsAny<EntityTemplate>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_WhenDuplicateWithDifferentCasing_ShouldStillDetectDuplicate()
    {
        // Arrange
        SetupGameSystem();
        
        // Setup existing template check with normalized name
        _templateRepositoryMock
            .Setup(x => x.ExistsByEntityTypeNameAsync(
                _gameSystemId, 
                _ownerId, 
                "player_character", // Normalized
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var command = new CreateTemplateCommand(
            GameSystemId: _gameSystemId,
            OwnerId: _ownerId,
            EntityTypeName: "PLAYER CHARACTER", // Different casing
            DisplayName: "Player Character Template");

        // Act
        var act = () => _handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*template with entity type*already exists*");
    }

    #endregion

    #region Edge Cases

    [Fact]
    public async Task Handle_WithEmptyFieldsList_ShouldCreateTemplateWithNoFields()
    {
        // Arrange
        SetupGameSystem();
        SetupNoExistingTemplate("item");

        var command = new CreateTemplateCommand(
            GameSystemId: _gameSystemId,
            OwnerId: _ownerId,
            EntityTypeName: "Item",
            DisplayName: "Item Template",
            Fields: new List<FieldDefinitionDto>());

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.FieldCount.Should().Be(0);
    }

    [Fact]
    public async Task Handle_WithNullFields_ShouldCreateTemplateWithNoFields()
    {
        // Arrange
        SetupGameSystem();
        SetupNoExistingTemplate("location");

        var command = new CreateTemplateCommand(
            GameSystemId: _gameSystemId,
            OwnerId: _ownerId,
            EntityTypeName: "Location",
            DisplayName: "Location Template",
            Fields: null);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.FieldCount.Should().Be(0);
    }

    #endregion

    #region Helper Methods

    /// <summary>
    /// Sets up a valid game system for testing.
    /// </summary>
    private static readonly Guid TestOwnerId = Guid.NewGuid();

    private void SetupGameSystem()
    {
        var gameSystem = GameSystem.Create(
            "dnd5e",
            "Dungeons & Dragons 5th Edition",
            TestOwnerId,
            "Wizards of the Coast");

        _gameSystemRepositoryMock
            .Setup(x => x.GetByIdAsync(_gameSystemId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(gameSystem);
    }

    /// <summary>
    /// Sets up the repository to return that no template with this name exists.
    /// </summary>
    private void SetupNoExistingTemplate(string normalizedTypeName)
    {
        _templateRepositoryMock
            .Setup(x => x.ExistsByEntityTypeNameAsync(
                _gameSystemId,
                _ownerId,
                normalizedTypeName,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
    }

    /// <summary>
    /// Sets up the repository to return that a template with this name already exists.
    /// </summary>
    private void SetupExistingTemplate(string normalizedTypeName)
    {
        _templateRepositoryMock
            .Setup(x => x.ExistsByEntityTypeNameAsync(
                _gameSystemId,
                _ownerId,
                normalizedTypeName,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
    }

    #endregion
}
