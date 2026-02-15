using Loremaster.Application.Common.Interfaces;
using Loremaster.Application.Features.LoreEntities.Commands.TransferEntityOwnership;
using Loremaster.Domain.Entities;
using Loremaster.Domain.Enums;
using Loremaster.Domain.Exceptions;
using Microsoft.Extensions.Logging;

namespace Loremaster.Tests.Unit.Application.Features.LoreEntities;

/// <summary>
/// Unit tests for TransferEntityOwnershipCommandHandler.
/// Tests ownership transfers, permission checks, and ownership type assignment.
/// </summary>
public class TransferEntityOwnershipCommandHandlerTests
{
    private readonly Mock<ILoreEntityRepository> _loreEntityRepositoryMock;
    private readonly Mock<ICampaignMemberRepository> _campaignMemberRepositoryMock;
    private readonly Mock<ICurrentUserService> _currentUserServiceMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<ILogger<TransferEntityOwnershipCommandHandler>> _loggerMock;
    private readonly TransferEntityOwnershipCommandHandler _handler;

    private readonly Guid _masterId = Guid.NewGuid();
    private readonly Guid _playerId = Guid.NewGuid();
    private readonly Guid _otherPlayerId = Guid.NewGuid();
    private readonly Guid _campaignId = Guid.NewGuid();
    private readonly Guid _entityId = Guid.NewGuid();

    public TransferEntityOwnershipCommandHandlerTests()
    {
        _loreEntityRepositoryMock = new Mock<ILoreEntityRepository>();
        _campaignMemberRepositoryMock = new Mock<ICampaignMemberRepository>();
        _currentUserServiceMock = new Mock<ICurrentUserService>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _loggerMock = new Mock<ILogger<TransferEntityOwnershipCommandHandler>>();

        _handler = new TransferEntityOwnershipCommandHandler(
            _loreEntityRepositoryMock.Object,
            _campaignMemberRepositoryMock.Object,
            _currentUserServiceMock.Object,
            _unitOfWorkMock.Object,
            _loggerMock.Object);

        // Default setup - master is authenticated
        _currentUserServiceMock.Setup(x => x.UserId).Returns(_masterId);
    }

    #region Success Cases - Master Transfers to Player

    [Fact]
    public async Task Handle_WhenMasterTransfersToPlayer_ShouldTransferOwnership()
    {
        // Arrange
        var masterMembership = CreateMasterMembership(_masterId);
        var playerMembership = CreatePlayerMembership(_playerId);
        var entity = CreateEntity(_masterId, OwnershipType.Master);

        SetupCurrentUserMembership(masterMembership);
        SetupNewOwnerMembership(playerMembership);
        SetupEntity(entity);

        var command = new TransferEntityOwnershipCommand(
            _campaignId,
            _entityId,
            _playerId);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.OwnerId.Should().Be(_playerId);
        result.OwnershipType.Should().Be(OwnershipType.Player); // Defaults to Player for player transfer

        _loreEntityRepositoryMock.Verify(x => x.Update(It.IsAny<LoreEntity>()), Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenMasterTransfersWithExplicitOwnershipType_ShouldUseSpecifiedType()
    {
        // Arrange
        var masterMembership = CreateMasterMembership(_masterId);
        var playerMembership = CreatePlayerMembership(_playerId);
        var entity = CreateEntity(_masterId, OwnershipType.Master);

        SetupCurrentUserMembership(masterMembership);
        SetupNewOwnerMembership(playerMembership);
        SetupEntity(entity);

        var command = new TransferEntityOwnershipCommand(
            _campaignId,
            _entityId,
            _playerId,
            OwnershipType.Shared); // Explicit shared ownership

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.OwnershipType.Should().Be(OwnershipType.Shared);
    }

    [Fact]
    public async Task Handle_WhenMasterTransfersToAnotherMaster_ShouldDefaultToMasterOwnership()
    {
        // Arrange
        var anotherMasterId = Guid.NewGuid();
        var masterMembership = CreateMasterMembership(_masterId);
        var anotherMasterMembership = CreateMasterMembership(anotherMasterId);
        var entity = CreateEntity(_masterId, OwnershipType.Master);

        SetupCurrentUserMembership(masterMembership);
        SetupNewOwnerMembership(anotherMasterMembership);
        SetupEntity(entity);

        var command = new TransferEntityOwnershipCommand(
            _campaignId,
            _entityId,
            anotherMasterId);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.OwnerId.Should().Be(anotherMasterId);
        result.OwnershipType.Should().Be(OwnershipType.Master);
    }

    [Fact]
    public async Task Handle_WhenMasterTransfersPlayerEntityBackToSelf_ShouldSucceed()
    {
        // Arrange - Master takes back an entity from a player
        var masterMembership = CreateMasterMembership(_masterId);
        var entity = CreateEntity(_playerId, OwnershipType.Player);

        SetupCurrentUserMembership(masterMembership);
        SetupNewOwnerMembership(masterMembership); // Transfer to self
        SetupEntity(entity);

        var command = new TransferEntityOwnershipCommand(
            _campaignId,
            _entityId,
            _masterId,
            OwnershipType.Master);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.OwnerId.Should().Be(_masterId);
        result.OwnershipType.Should().Be(OwnershipType.Master);
    }

    [Fact]
    public async Task Handle_WhenMasterTransfersSharedEntity_ShouldSucceed()
    {
        // Arrange
        var masterMembership = CreateMasterMembership(_masterId);
        var playerMembership = CreatePlayerMembership(_playerId);
        var entity = CreateEntity(_masterId, OwnershipType.Shared);

        SetupCurrentUserMembership(masterMembership);
        SetupNewOwnerMembership(playerMembership);
        SetupEntity(entity);

        var command = new TransferEntityOwnershipCommand(
            _campaignId,
            _entityId,
            _playerId);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.OwnerId.Should().Be(_playerId);
    }

    [Fact]
    public async Task Handle_WhenMasterTransfersBetweenPlayers_ShouldSucceed()
    {
        // Arrange - Master transfers player's entity to another player
        var masterMembership = CreateMasterMembership(_masterId);
        var otherPlayerMembership = CreatePlayerMembership(_otherPlayerId);
        var entity = CreateEntity(_playerId, OwnershipType.Player);

        SetupCurrentUserMembership(masterMembership);
        SetupNewOwnerMembership(otherPlayerMembership);
        SetupEntity(entity);

        var command = new TransferEntityOwnershipCommand(
            _campaignId,
            _entityId,
            _otherPlayerId);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.OwnerId.Should().Be(_otherPlayerId);
        result.OwnershipType.Should().Be(OwnershipType.Player);
    }

    #endregion

    #region Authentication Failures

    [Fact]
    public async Task Handle_WhenNotAuthenticated_ShouldThrowForbiddenAccessException()
    {
        // Arrange
        _currentUserServiceMock.Setup(x => x.UserId).Returns((Guid?)null);

        var command = new TransferEntityOwnershipCommand(
            _campaignId,
            _entityId,
            _playerId);

        // Act
        var act = () => _handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ForbiddenAccessException>()
            .WithMessage("*authenticated*");
    }

    #endregion

    #region Authorization Failures - Master or Owner Can Transfer

    [Fact]
    public async Task Handle_WhenPlayerTriesToTransferOthersEntity_ShouldThrowForbiddenAccessException()
    {
        // Arrange - Player tries to transfer an entity they don't own
        _currentUserServiceMock.Setup(x => x.UserId).Returns(_playerId);
        var playerMembership = CreatePlayerMembership(_playerId);
        var entity = CreateEntity(_otherPlayerId, OwnershipType.Player); // Entity owned by other player

        SetupCurrentUserMembership(playerMembership);
        SetupEntity(entity);

        var command = new TransferEntityOwnershipCommand(
            _campaignId,
            _entityId,
            _masterId);

        // Act
        var act = () => _handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ForbiddenAccessException>()
            .WithMessage("*master*owner*");
    }

    [Fact]
    public async Task Handle_WhenPlayerTransfersOwnEntity_ShouldSucceed()
    {
        // Arrange - Player transfers their own entity to another player
        _currentUserServiceMock.Setup(x => x.UserId).Returns(_playerId);
        var playerMembership = CreatePlayerMembership(_playerId);
        var otherPlayerMembership = CreatePlayerMembership(_otherPlayerId);
        var entity = CreateEntity(_playerId, OwnershipType.Player);

        SetupCurrentUserMembership(playerMembership);
        SetupNewOwnerMembership(otherPlayerMembership);
        SetupEntity(entity);

        var command = new TransferEntityOwnershipCommand(
            _campaignId,
            _entityId,
            _otherPlayerId);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.OwnerId.Should().Be(_otherPlayerId);
        result.OwnershipType.Should().Be(OwnershipType.Player);

        _loreEntityRepositoryMock.Verify(x => x.Update(It.IsAny<LoreEntity>()), Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenPlayerTransfersOwnEntityToMaster_ShouldSucceed()
    {
        // Arrange - Player transfers their entity to the master
        _currentUserServiceMock.Setup(x => x.UserId).Returns(_playerId);
        var playerMembership = CreatePlayerMembership(_playerId);
        var masterMembership = CreateMasterMembership(_masterId);
        var entity = CreateEntity(_playerId, OwnershipType.Player);

        SetupCurrentUserMembership(playerMembership);
        SetupNewOwnerMembership(masterMembership);
        SetupEntity(entity);

        var command = new TransferEntityOwnershipCommand(
            _campaignId,
            _entityId,
            _masterId);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.OwnerId.Should().Be(_masterId);
        result.OwnershipType.Should().Be(OwnershipType.Master);
    }

    #endregion

    #region Not Found Errors

    [Fact]
    public async Task Handle_WhenNotCampaignMember_ShouldThrowNotFoundException()
    {
        // Arrange
        _campaignMemberRepositoryMock
            .Setup(x => x.GetByCampaignAndUserAsync(_campaignId, _masterId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((CampaignMember?)null);

        var command = new TransferEntityOwnershipCommand(
            _campaignId,
            _entityId,
            _playerId);

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
        var masterMembership = CreateMasterMembership(_masterId);
        SetupCurrentUserMembership(masterMembership);

        _loreEntityRepositoryMock
            .Setup(x => x.GetByIdAsync(_entityId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((LoreEntity?)null);

        var command = new TransferEntityOwnershipCommand(
            _campaignId,
            _entityId,
            _playerId);

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
        var masterMembership = CreateMasterMembership(_masterId);
        SetupCurrentUserMembership(masterMembership);

        var entityInOtherCampaign = LoreEntity.Create(
            campaignId: Guid.NewGuid(), // Different campaign
            ownerId: _masterId,
            entityType: "character",
            name: "Test",
            ownershipType: OwnershipType.Master);

        _loreEntityRepositoryMock
            .Setup(x => x.GetByIdAsync(_entityId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(entityInOtherCampaign);

        var command = new TransferEntityOwnershipCommand(
            _campaignId,
            _entityId,
            _playerId);

        // Act
        var act = () => _handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<NotFoundException>()
            .WithMessage("*LoreEntity*");
    }

    [Fact]
    public async Task Handle_WhenNewOwnerNotCampaignMember_ShouldThrowNotFoundException()
    {
        // Arrange
        var masterMembership = CreateMasterMembership(_masterId);
        var entity = CreateEntity(_masterId, OwnershipType.Master);

        SetupCurrentUserMembership(masterMembership);
        SetupEntity(entity);

        // New owner is not a campaign member
        _campaignMemberRepositoryMock
            .Setup(x => x.GetByCampaignAndUserAsync(_campaignId, _playerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((CampaignMember?)null);

        var command = new TransferEntityOwnershipCommand(
            _campaignId,
            _entityId,
            _playerId);

        // Act
        var act = () => _handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<NotFoundException>()
            .WithMessage("*CampaignMember*");
    }

    #endregion

    #region Edge Cases

    [Fact]
    public async Task Handle_WhenTransferringToCurrentOwner_ShouldSucceed()
    {
        // Arrange - Transfer to same owner (basically a no-op, but should work)
        var masterMembership = CreateMasterMembership(_masterId);
        var entity = CreateEntity(_masterId, OwnershipType.Master);

        SetupCurrentUserMembership(masterMembership);
        SetupNewOwnerMembership(masterMembership);
        SetupEntity(entity);

        var command = new TransferEntityOwnershipCommand(
            _campaignId,
            _entityId,
            _masterId);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.OwnerId.Should().Be(_masterId);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenTransferringTemplate_ShouldSucceed()
    {
        // Arrange
        var masterMembership = CreateMasterMembership(_masterId);
        var playerMembership = CreatePlayerMembership(_playerId);
        var templateEntity = LoreEntity.Create(
            campaignId: _campaignId,
            ownerId: _masterId,
            entityType: "character",
            name: "Template Character",
            ownershipType: OwnershipType.Master,
            isTemplate: true);

        SetupCurrentUserMembership(masterMembership);
        SetupNewOwnerMembership(playerMembership);
        _loreEntityRepositoryMock
            .Setup(x => x.GetByIdAsync(_entityId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(templateEntity);

        var command = new TransferEntityOwnershipCommand(
            _campaignId,
            _entityId,
            _playerId);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.OwnerId.Should().Be(_playerId);
        result.IsTemplate.Should().BeTrue();
    }

    #endregion

    #region Helper Methods

    private CampaignMember CreateMasterMembership(Guid userId)
    {
        return CampaignMember.Create(_campaignId, userId, CampaignRole.Master);
    }

    private CampaignMember CreatePlayerMembership(Guid userId)
    {
        return CampaignMember.Create(_campaignId, userId, CampaignRole.Player);
    }

    private LoreEntity CreateEntity(Guid ownerId, OwnershipType ownershipType)
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

    private void SetupCurrentUserMembership(CampaignMember membership)
    {
        var userId = _currentUserServiceMock.Object.UserId!.Value;
        _campaignMemberRepositoryMock
            .Setup(x => x.GetByCampaignAndUserAsync(_campaignId, userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(membership);
    }

    private void SetupNewOwnerMembership(CampaignMember membership)
    {
        _campaignMemberRepositoryMock
            .Setup(x => x.GetByCampaignAndUserAsync(_campaignId, membership.UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(membership);
    }

    private void SetupEntity(LoreEntity entity)
    {
        _loreEntityRepositoryMock
            .Setup(x => x.GetByIdAsync(_entityId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(entity);
    }

    #endregion
}
