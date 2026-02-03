using Loremaster.Application.Common.Interfaces;
using Loremaster.Application.Features.LoreEntities.Commands.DeleteLoreEntity;
using Loremaster.Domain.Entities;
using Loremaster.Domain.Enums;
using Loremaster.Domain.Exceptions;
using Microsoft.Extensions.Logging;
using MediatRUnit = MediatR.Unit;

namespace Loremaster.Tests.Unit.Application.Features.LoreEntities;

/// <summary>
/// Unit tests for DeleteLoreEntityCommandHandler.
/// Tests entity deletion, permission checks, and ownership rules.
/// </summary>
public class DeleteLoreEntityCommandHandlerTests
{
    private readonly Mock<ILoreEntityRepository> _loreEntityRepositoryMock;
    private readonly Mock<ICampaignMemberRepository> _campaignMemberRepositoryMock;
    private readonly Mock<ICurrentUserService> _currentUserServiceMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<ILogger<DeleteLoreEntityCommandHandler>> _loggerMock;
    private readonly DeleteLoreEntityCommandHandler _handler;

    private readonly Guid _userId = Guid.NewGuid();
    private readonly Guid _otherUserId = Guid.NewGuid();
    private readonly Guid _campaignId = Guid.NewGuid();
    private readonly Guid _entityId = Guid.NewGuid();

    public DeleteLoreEntityCommandHandlerTests()
    {
        _loreEntityRepositoryMock = new Mock<ILoreEntityRepository>();
        _campaignMemberRepositoryMock = new Mock<ICampaignMemberRepository>();
        _currentUserServiceMock = new Mock<ICurrentUserService>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _loggerMock = new Mock<ILogger<DeleteLoreEntityCommandHandler>>();

        _handler = new DeleteLoreEntityCommandHandler(
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
    public async Task Handle_WhenOwnerDeletes_ShouldDeleteEntity()
    {
        // Arrange
        var membership = CreateMasterMembership();
        var entity = CreateMasterOwnedEntity();
        SetupMocks(membership, entity);

        var command = new DeleteLoreEntityCommand(
            CampaignId: _campaignId,
            EntityId: _entityId);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().Be(MediatRUnit.Value);
        _loreEntityRepositoryMock.Verify(x => x.Delete(entity), Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenMasterDeletesMasterEntity_ShouldSucceed()
    {
        // Arrange - Master deletes another master's entity
        var masterMembership = CreateMasterMembership();
        var entity = CreateEntityWithOwner(_otherUserId, OwnershipType.Master);
        SetupMocks(masterMembership, entity);

        var command = new DeleteLoreEntityCommand(
            CampaignId: _campaignId,
            EntityId: _entityId);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().Be(MediatRUnit.Value);
        _loreEntityRepositoryMock.Verify(x => x.Delete(entity), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenMasterDeletesSharedEntity_ShouldSucceed()
    {
        // Arrange
        var masterMembership = CreateMasterMembership();
        var entity = CreateEntityWithOwner(_otherUserId, OwnershipType.Shared);
        SetupMocks(masterMembership, entity);

        var command = new DeleteLoreEntityCommand(
            CampaignId: _campaignId,
            EntityId: _entityId);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().Be(MediatRUnit.Value);
        _loreEntityRepositoryMock.Verify(x => x.Delete(entity), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenPlayerDeletesOwnEntity_ShouldSucceed()
    {
        // Arrange
        var playerMembership = CreatePlayerMembership();
        var entity = CreatePlayerOwnedEntity();
        SetupMocks(playerMembership, entity);

        var command = new DeleteLoreEntityCommand(
            CampaignId: _campaignId,
            EntityId: _entityId);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().Be(MediatRUnit.Value);
        _loreEntityRepositoryMock.Verify(x => x.Delete(entity), Times.Once);
    }

    #endregion

    #region Authentication and Authorization Failures

    [Fact]
    public async Task Handle_WhenNotAuthenticated_ShouldThrowForbiddenAccessException()
    {
        // Arrange
        _currentUserServiceMock.Setup(x => x.UserId).Returns((Guid?)null);

        var command = new DeleteLoreEntityCommand(
            CampaignId: _campaignId,
            EntityId: _entityId);

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

        var command = new DeleteLoreEntityCommand(
            CampaignId: _campaignId,
            EntityId: _entityId);

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

        var command = new DeleteLoreEntityCommand(
            CampaignId: _campaignId,
            EntityId: _entityId);

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

        var command = new DeleteLoreEntityCommand(
            CampaignId: _campaignId,
            EntityId: _entityId);

        // Act
        var act = () => _handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<NotFoundException>()
            .WithMessage("*LoreEntity*");
    }

    [Fact]
    public async Task Handle_WhenMasterDeletesPlayerEntity_ShouldThrowForbiddenAccessException()
    {
        // Arrange - Master tries to delete player-owned entity
        var masterMembership = CreateMasterMembership();
        var playerEntity = CreateEntityWithOwner(_otherUserId, OwnershipType.Player);
        SetupMocks(masterMembership, playerEntity);

        var command = new DeleteLoreEntityCommand(
            CampaignId: _campaignId,
            EntityId: _entityId);

        // Act
        var act = () => _handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ForbiddenAccessException>()
            .WithMessage("*permission*");
    }

    [Fact]
    public async Task Handle_WhenPlayerDeletesOtherPlayerEntity_ShouldThrowForbiddenAccessException()
    {
        // Arrange
        var playerMembership = CreatePlayerMembership();
        var otherPlayerEntity = CreateEntityWithOwner(_otherUserId, OwnershipType.Player);
        SetupMocks(playerMembership, otherPlayerEntity);

        var command = new DeleteLoreEntityCommand(
            CampaignId: _campaignId,
            EntityId: _entityId);

        // Act
        var act = () => _handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ForbiddenAccessException>()
            .WithMessage("*permission*");
    }

    [Fact]
    public async Task Handle_WhenPlayerDeletesMasterEntity_ShouldThrowForbiddenAccessException()
    {
        // Arrange
        var playerMembership = CreatePlayerMembership();
        var masterEntity = CreateEntityWithOwner(_otherUserId, OwnershipType.Master);
        SetupMocks(playerMembership, masterEntity);

        var command = new DeleteLoreEntityCommand(
            CampaignId: _campaignId,
            EntityId: _entityId);

        // Act
        var act = () => _handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ForbiddenAccessException>()
            .WithMessage("*permission*");
    }

    [Fact]
    public async Task Handle_WhenPlayerDeletesSharedEntity_ShouldThrowForbiddenAccessException()
    {
        // Arrange
        var playerMembership = CreatePlayerMembership();
        var sharedEntity = CreateEntityWithOwner(_otherUserId, OwnershipType.Shared);
        SetupMocks(playerMembership, sharedEntity);

        var command = new DeleteLoreEntityCommand(
            CampaignId: _campaignId,
            EntityId: _entityId);

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
