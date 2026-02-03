using Loremaster.Application.Common.Interfaces;
using Loremaster.Application.Features.LoreEntities.Commands.UpdateLoreEntity;
using Loremaster.Domain.Entities;
using Loremaster.Domain.Enums;
using Loremaster.Domain.Exceptions;
using Microsoft.Extensions.Logging;

namespace Loremaster.Tests.Unit.Application.Features.LoreEntities;

/// <summary>
/// Unit tests for UpdateLoreEntityCommandHandler.
/// Tests entity updates, permission checks, and ownership rules.
/// </summary>
public class UpdateLoreEntityCommandHandlerTests
{
    private readonly Mock<ILoreEntityRepository> _loreEntityRepositoryMock;
    private readonly Mock<ICampaignMemberRepository> _campaignMemberRepositoryMock;
    private readonly Mock<ICurrentUserService> _currentUserServiceMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<ILogger<UpdateLoreEntityCommandHandler>> _loggerMock;
    private readonly UpdateLoreEntityCommandHandler _handler;

    private readonly Guid _userId = Guid.NewGuid();
    private readonly Guid _otherUserId = Guid.NewGuid();
    private readonly Guid _campaignId = Guid.NewGuid();
    private readonly Guid _entityId = Guid.NewGuid();

    public UpdateLoreEntityCommandHandlerTests()
    {
        _loreEntityRepositoryMock = new Mock<ILoreEntityRepository>();
        _campaignMemberRepositoryMock = new Mock<ICampaignMemberRepository>();
        _currentUserServiceMock = new Mock<ICurrentUserService>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _loggerMock = new Mock<ILogger<UpdateLoreEntityCommandHandler>>();

        _handler = new UpdateLoreEntityCommandHandler(
            _loreEntityRepositoryMock.Object,
            _campaignMemberRepositoryMock.Object,
            _currentUserServiceMock.Object,
            _unitOfWorkMock.Object,
            _loggerMock.Object);

        // Default setup - authenticated user
        _currentUserServiceMock.Setup(x => x.UserId).Returns(_userId);
    }

    #region Success Cases

    [Fact]
    public async Task Handle_WhenOwnerUpdates_ShouldUpdateEntity()
    {
        // Arrange
        var membership = CreateMasterMembership();
        var entity = CreateMasterOwnedEntity();
        SetupMocks(membership, entity);

        var command = new UpdateLoreEntityCommand(
            CampaignId: _campaignId,
            EntityId: _entityId,
            Name: "Updated Name",
            Description: "Updated Description");

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Name.Should().Be("Updated Name");
        result.Description.Should().Be("Updated Description");

        _loreEntityRepositoryMock.Verify(x => x.Update(It.IsAny<LoreEntity>()), Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenMasterUpdatesMasterEntity_ShouldSucceed()
    {
        // Arrange - Master updates another master's entity (Master ownership)
        var masterMembership = CreateMasterMembership();
        var entity = CreateEntityWithOwner(_otherUserId, OwnershipType.Master);
        SetupMocks(masterMembership, entity);

        var command = new UpdateLoreEntityCommand(
            CampaignId: _campaignId,
            EntityId: _entityId,
            Name: "Master Updated");

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Name.Should().Be("Master Updated");
    }

    [Fact]
    public async Task Handle_WhenMasterUpdatesSharedEntity_ShouldSucceed()
    {
        // Arrange
        var masterMembership = CreateMasterMembership();
        var entity = CreateEntityWithOwner(_otherUserId, OwnershipType.Shared);
        SetupMocks(masterMembership, entity);

        var command = new UpdateLoreEntityCommand(
            CampaignId: _campaignId,
            EntityId: _entityId,
            Name: "Shared Updated");

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
    }

    [Fact]
    public async Task Handle_WhenPlayerUpdatesOwnEntity_ShouldSucceed()
    {
        // Arrange
        var playerMembership = CreatePlayerMembership();
        var entity = CreatePlayerOwnedEntity();
        SetupMocks(playerMembership, entity);

        var command = new UpdateLoreEntityCommand(
            CampaignId: _campaignId,
            EntityId: _entityId,
            Name: "Player Updated");

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Name.Should().Be("Player Updated");
    }

    [Fact]
    public async Task Handle_WithVisibilityChange_ShouldUpdateVisibility()
    {
        // Arrange
        var membership = CreateMasterMembership();
        var entity = CreateMasterOwnedEntity();
        SetupMocks(membership, entity);

        var command = new UpdateLoreEntityCommand(
            CampaignId: _campaignId,
            EntityId: _entityId,
            Name: "Test",
            Visibility: VisibilityLevel.Public);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Visibility.Should().Be(VisibilityLevel.Public);
    }

    [Fact]
    public async Task Handle_WithAttributes_ShouldUpdateAttributes()
    {
        // Arrange
        var membership = CreateMasterMembership();
        var entity = CreateMasterOwnedEntity();
        SetupMocks(membership, entity);

        var attributes = new Dictionary<string, object>
        {
            { "strength", 20 },
            { "newAttribute", "value" }
        };

        var command = new UpdateLoreEntityCommand(
            CampaignId: _campaignId,
            EntityId: _entityId,
            Name: "Test",
            Attributes: attributes);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion

    #region Authentication and Authorization Failures

    [Fact]
    public async Task Handle_WhenNotAuthenticated_ShouldThrowForbiddenAccessException()
    {
        // Arrange
        _currentUserServiceMock.Setup(x => x.UserId).Returns((Guid?)null);

        var command = new UpdateLoreEntityCommand(
            CampaignId: _campaignId,
            EntityId: _entityId,
            Name: "Test");

        // Act
        var act = () => _handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ForbiddenAccessException>()
            .WithMessage("*authenticated*");
    }

    [Fact]
    public async Task Handle_WhenNotCampaignMember_ShouldThrowNotFoundException()
    {
        // Arrange
        _campaignMemberRepositoryMock
            .Setup(x => x.GetByCampaignAndUserAsync(_campaignId, _userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((CampaignMember?)null);

        var command = new UpdateLoreEntityCommand(
            CampaignId: _campaignId,
            EntityId: _entityId,
            Name: "Test");

        // Act
        var act = () => _handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<NotFoundException>()
            .WithMessage("*Campaign*");
    }

    [Fact]
    public async Task Handle_WhenEntityNotFound_ShouldThrowNotFoundException()
    {
        // Arrange
        var membership = CreateMasterMembership();
        SetupCampaignMembership(membership);
        
        _loreEntityRepositoryMock
            .Setup(x => x.GetByIdAsync(_entityId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((LoreEntity?)null);

        var command = new UpdateLoreEntityCommand(
            CampaignId: _campaignId,
            EntityId: _entityId,
            Name: "Test");

        // Act
        var act = () => _handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<NotFoundException>()
            .WithMessage("*LoreEntity*");
    }

    [Fact]
    public async Task Handle_WhenEntityInDifferentCampaign_ShouldThrowNotFoundException()
    {
        // Arrange
        var membership = CreateMasterMembership();
        SetupCampaignMembership(membership);
        
        var entityInOtherCampaign = LoreEntity.Create(
            campaignId: Guid.NewGuid(), // Different campaign
            ownerId: _userId,
            entityType: "character",
            name: "Test",
            ownershipType: OwnershipType.Master);

        _loreEntityRepositoryMock
            .Setup(x => x.GetByIdAsync(_entityId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(entityInOtherCampaign);

        var command = new UpdateLoreEntityCommand(
            CampaignId: _campaignId,
            EntityId: _entityId,
            Name: "Test");

        // Act
        var act = () => _handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<NotFoundException>()
            .WithMessage("*LoreEntity*");
    }

    [Fact]
    public async Task Handle_WhenMasterUpdatesPlayerEntity_ShouldThrowForbiddenAccessException()
    {
        // Arrange - Master tries to update player-owned entity
        var masterMembership = CreateMasterMembership();
        var playerEntity = CreateEntityWithOwner(_otherUserId, OwnershipType.Player);
        SetupMocks(masterMembership, playerEntity);

        var command = new UpdateLoreEntityCommand(
            CampaignId: _campaignId,
            EntityId: _entityId,
            Name: "Should Fail");

        // Act
        var act = () => _handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ForbiddenAccessException>()
            .WithMessage("*permission*");
    }

    [Fact]
    public async Task Handle_WhenPlayerUpdatesOtherPlayerEntity_ShouldThrowForbiddenAccessException()
    {
        // Arrange
        var playerMembership = CreatePlayerMembership();
        var otherPlayerEntity = CreateEntityWithOwner(_otherUserId, OwnershipType.Player);
        SetupMocks(playerMembership, otherPlayerEntity);

        var command = new UpdateLoreEntityCommand(
            CampaignId: _campaignId,
            EntityId: _entityId,
            Name: "Should Fail");

        // Act
        var act = () => _handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ForbiddenAccessException>()
            .WithMessage("*permission*");
    }

    [Fact]
    public async Task Handle_WhenPlayerUpdatesMasterEntity_ShouldThrowForbiddenAccessException()
    {
        // Arrange
        var playerMembership = CreatePlayerMembership();
        var masterEntity = CreateEntityWithOwner(_otherUserId, OwnershipType.Master);
        SetupMocks(playerMembership, masterEntity);

        var command = new UpdateLoreEntityCommand(
            CampaignId: _campaignId,
            EntityId: _entityId,
            Name: "Should Fail");

        // Act
        var act = () => _handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ForbiddenAccessException>()
            .WithMessage("*permission*");
    }

    #endregion

    #region Helper Methods

    private CampaignMember CreateMasterMembership()
    {
        return CampaignMember.Create(_campaignId, _userId, CampaignRole.Master);
    }

    private CampaignMember CreatePlayerMembership()
    {
        return CampaignMember.Create(_campaignId, _userId, CampaignRole.Player);
    }

    private LoreEntity CreateMasterOwnedEntity()
    {
        return CreateEntityWithOwner(_userId, OwnershipType.Master);
    }

    private LoreEntity CreatePlayerOwnedEntity()
    {
        return CreateEntityWithOwner(_userId, OwnershipType.Player);
    }

    private LoreEntity CreateEntityWithOwner(Guid ownerId, OwnershipType ownershipType)
    {
        return LoreEntity.Create(
            campaignId: _campaignId,
            ownerId: ownerId,
            entityType: "character",
            name: "Test Entity",
            description: "Test description",
            ownershipType: ownershipType,
            visibility: VisibilityLevel.Campaign);
    }

    private void SetupCampaignMembership(CampaignMember membership)
    {
        _campaignMemberRepositoryMock
            .Setup(x => x.GetByCampaignAndUserAsync(_campaignId, _userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(membership);
    }

    private void SetupMocks(CampaignMember membership, LoreEntity entity)
    {
        SetupCampaignMembership(membership);
        _loreEntityRepositoryMock
            .Setup(x => x.GetByIdAsync(_entityId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(entity);
    }

    #endregion
}
