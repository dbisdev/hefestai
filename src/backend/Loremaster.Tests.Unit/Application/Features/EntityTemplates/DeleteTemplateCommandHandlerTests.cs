using Loremaster.Application.Common.Interfaces;
using Loremaster.Application.Features.EntityTemplates.Commands.DeleteTemplate;
using Loremaster.Domain.Entities;
using Loremaster.Domain.ValueObjects;
using Microsoft.Extensions.Logging;

namespace Loremaster.Tests.Unit.Application.Features.EntityTemplates;

/// <summary>
/// Unit tests for DeleteTemplateCommandHandler.
/// Tests template deletion, ownership validation, and entity-in-use protection.
/// </summary>
public class DeleteTemplateCommandHandlerTests
{
    private readonly Mock<IEntityTemplateRepository> _templateRepositoryMock;
    private readonly Mock<ILoreEntityRepository> _entityRepositoryMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<ILogger<DeleteTemplateCommandHandler>> _loggerMock;
    private readonly DeleteTemplateCommandHandler _handler;

    private readonly Guid _ownerId = Guid.NewGuid();
    private readonly Guid _otherUserId = Guid.NewGuid();
    private readonly Guid _templateId = Guid.NewGuid();
    private readonly Guid _gameSystemId = Guid.NewGuid();

    public DeleteTemplateCommandHandlerTests()
    {
        _templateRepositoryMock = new Mock<IEntityTemplateRepository>();
        _entityRepositoryMock = new Mock<ILoreEntityRepository>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _loggerMock = new Mock<ILogger<DeleteTemplateCommandHandler>>();

        _handler = new DeleteTemplateCommandHandler(
            _templateRepositoryMock.Object,
            _entityRepositoryMock.Object,
            _unitOfWorkMock.Object,
            _loggerMock.Object);
    }

    #region Success Cases

    [Fact]
    public async Task Handle_WhenNoEntitiesUseTemplate_ShouldDeleteSuccessfully()
    {
        // Arrange
        var template = CreateTemplate("character", _ownerId);
        SetupTemplate(template);
        SetupEntityCount(0);

        var command = new DeleteTemplateCommand(
            TemplateId: _templateId,
            OwnerId: _ownerId);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.TemplateId.Should().Be(_templateId);
        result.EntityTypeName.Should().Be("character");
        result.WasForced.Should().BeFalse();
        result.AffectedEntityCount.Should().Be(0);

        _templateRepositoryMock.Verify(
            x => x.Delete(It.IsAny<EntityTemplate>()),
            Times.Once);
        _unitOfWorkMock.Verify(
            x => x.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WithForceDelete_ShouldDeleteEvenWithEntities()
    {
        // Arrange
        var template = CreateTemplate("vehicle", _ownerId);
        SetupTemplate(template);
        SetupEntityCount(5); // 5 entities using this template

        var command = new DeleteTemplateCommand(
            TemplateId: _templateId,
            OwnerId: _ownerId,
            ForceDelete: true);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.WasForced.Should().BeTrue();
        result.AffectedEntityCount.Should().Be(5);

        _templateRepositoryMock.Verify(
            x => x.Delete(It.IsAny<EntityTemplate>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WithForceDeleteButNoEntities_ShouldNotBeMarkedAsForced()
    {
        // Arrange
        var template = CreateTemplate("item", _ownerId);
        SetupTemplate(template);
        SetupEntityCount(0);

        var command = new DeleteTemplateCommand(
            TemplateId: _templateId,
            OwnerId: _ownerId,
            ForceDelete: true);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.WasForced.Should().BeFalse(); // Not forced because no entities existed
        result.AffectedEntityCount.Should().Be(0);
    }

    #endregion

    #region Authorization Failures

    [Fact]
    public async Task Handle_WhenTemplateNotFound_ShouldThrowArgumentException()
    {
        // Arrange
        _templateRepositoryMock
            .Setup(x => x.GetByIdAsync(_templateId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((EntityTemplate?)null);

        var command = new DeleteTemplateCommand(
            TemplateId: _templateId,
            OwnerId: _ownerId);

        // Act
        var act = () => _handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage($"*Template with ID {_templateId} not found*");

        _templateRepositoryMock.Verify(
            x => x.Delete(It.IsAny<EntityTemplate>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_WhenNotOwner_ShouldThrowUnauthorizedAccessException()
    {
        // Arrange
        var template = CreateTemplate("character", _otherUserId); // Owned by another user
        SetupTemplate(template);

        var command = new DeleteTemplateCommand(
            TemplateId: _templateId,
            OwnerId: _ownerId); // Different user trying to delete

        // Act
        var act = () => _handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<UnauthorizedAccessException>()
            .WithMessage("*do not have permission*delete*template*");

        _templateRepositoryMock.Verify(
            x => x.Delete(It.IsAny<EntityTemplate>()),
            Times.Never);
    }

    #endregion

    #region Entity-In-Use Protection

    [Fact]
    public async Task Handle_WhenEntitiesExistAndNoForce_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var template = CreateTemplate("npc", _ownerId);
        SetupTemplate(template);
        SetupEntityCount(3); // 3 entities using this template

        var command = new DeleteTemplateCommand(
            TemplateId: _templateId,
            OwnerId: _ownerId,
            ForceDelete: false);

        // Act
        var act = () => _handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Cannot delete template*3 entities are using it*force delete*");

        _templateRepositoryMock.Verify(
            x => x.Delete(It.IsAny<EntityTemplate>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_WithSingleEntityAndNoForce_ShouldThrowWithCorrectMessage()
    {
        // Arrange
        var template = CreateTemplate("location", _ownerId);
        SetupTemplate(template);
        SetupEntityCount(1); // 1 entity using this template

        var command = new DeleteTemplateCommand(
            TemplateId: _templateId,
            OwnerId: _ownerId,
            ForceDelete: false);

        // Act
        var act = () => _handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*1 entities are using it*");
    }

    [Theory]
    [InlineData(1)]
    [InlineData(5)]
    [InlineData(100)]
    public async Task Handle_WithVariousEntityCounts_ShouldReportCorrectCount(int entityCount)
    {
        // Arrange
        var template = CreateTemplate("faction", _ownerId);
        SetupTemplate(template);
        SetupEntityCount(entityCount);

        var command = new DeleteTemplateCommand(
            TemplateId: _templateId,
            OwnerId: _ownerId,
            ForceDelete: true);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.AffectedEntityCount.Should().Be(entityCount);
        result.WasForced.Should().BeTrue();
    }

    #endregion

    #region Helper Methods

    /// <summary>
    /// Creates a test template with specified entity type and owner.
    /// </summary>
    private EntityTemplate CreateTemplate(string entityTypeName, Guid ownerId)
    {
        var template = EntityTemplate.Create(
            entityTypeName,
            $"{entityTypeName} Template",
            _gameSystemId,
            ownerId);
        
        // Add a field so it can be confirmed if needed
        template.SetFieldDefinitions(new[] { FieldDefinition.Text("name", "Name") });
        
        return template;
    }

    /// <summary>
    /// Sets up the repository to return the specified template.
    /// </summary>
    private void SetupTemplate(EntityTemplate template)
    {
        _templateRepositoryMock
            .Setup(x => x.GetByIdAsync(_templateId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(template);
    }

    /// <summary>
    /// Sets up the entity repository to return the specified count of entities using the template.
    /// </summary>
    private void SetupEntityCount(int count)
    {
        _entityRepositoryMock
            .Setup(x => x.CountByEntityTypeAsync(
                _gameSystemId,
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(count);
    }

    #endregion
}
