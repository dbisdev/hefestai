using Loremaster.Application.Common.Interfaces;
using Loremaster.Application.Features.LoreEntities.Commands.CreateLoreEntity;
using Loremaster.Domain.Entities;
using Loremaster.Domain.Enums;
using Loremaster.Domain.Exceptions;
using Loremaster.Domain.ValueObjects;
using Microsoft.Extensions.Logging;

namespace Loremaster.Tests.Unit.Application.Features.LoreEntities;

/// <summary>
/// Unit tests for CreateLoreEntityCommandHandler.
/// Tests entity creation, ownership rules, campaign membership validation, 
/// and entity template validation (EPIC 4 - Entity Definitions).
/// </summary>
public class CreateLoreEntityCommandHandlerTests
{
    private readonly Mock<ILoreEntityRepository> _loreEntityRepositoryMock;
    private readonly Mock<ICampaignMemberRepository> _campaignMemberRepositoryMock;
    private readonly Mock<ICampaignRepository> _campaignRepositoryMock;
    private readonly Mock<IEntityTemplateRepository> _entityTemplateRepositoryMock;
    private readonly Mock<ICurrentUserService> _currentUserServiceMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<ILogger<CreateLoreEntityCommandHandler>> _loggerMock;
    private readonly CreateLoreEntityCommandHandler _handler;

    private readonly Guid _userId = Guid.NewGuid();
    private readonly Guid _campaignOwnerId = Guid.NewGuid();
    private readonly Guid _campaignId = Guid.NewGuid();
    private readonly Guid _gameSystemId = Guid.NewGuid();

    public CreateLoreEntityCommandHandlerTests()
    {
        _loreEntityRepositoryMock = new Mock<ILoreEntityRepository>();
        _campaignMemberRepositoryMock = new Mock<ICampaignMemberRepository>();
        _campaignRepositoryMock = new Mock<ICampaignRepository>();
        _entityTemplateRepositoryMock = new Mock<IEntityTemplateRepository>();
        _currentUserServiceMock = new Mock<ICurrentUserService>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _loggerMock = new Mock<ILogger<CreateLoreEntityCommandHandler>>();

        _handler = new CreateLoreEntityCommandHandler(
            _loreEntityRepositoryMock.Object,
            _campaignMemberRepositoryMock.Object,
            _campaignRepositoryMock.Object,
            _entityTemplateRepositoryMock.Object,
            _currentUserServiceMock.Object,
            _unitOfWorkMock.Object,
            _loggerMock.Object);

        // Default setup - authenticated user
        _currentUserServiceMock.Setup(x => x.UserId).Returns(_userId);
    }

    #region Success Cases

    [Fact]
    public async Task Handle_WhenMasterCreatesEntity_ShouldCreateWithMasterOwnership()
    {
        // Arrange
        var membership = CreateMasterMembership();
        SetupSuccessScenario(membership, "character");

        var command = new CreateLoreEntityCommand(
            CampaignId: _campaignId,
            EntityType: "character",
            Name: "Test Character",
            Description: "A brave adventurer");

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Name.Should().Be(command.Name);
        result.Description.Should().Be(command.Description);
        result.EntityType.Should().Be("character");
        result.OwnershipType.Should().Be(OwnershipType.Master);
        result.Visibility.Should().Be(VisibilityLevel.Campaign);
        result.OwnerId.Should().Be(_userId);
        result.CampaignId.Should().Be(_campaignId);

        _loreEntityRepositoryMock.Verify(
            x => x.AddAsync(It.IsAny<LoreEntity>(), It.IsAny<CancellationToken>()), 
            Times.Once);
        _unitOfWorkMock.Verify(
            x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), 
            Times.Once);
    }

    [Fact]
    public async Task Handle_WhenPlayerCreatesEntity_ShouldCreateWithPlayerOwnership()
    {
        // Arrange
        var membership = CreatePlayerMembership();
        SetupSuccessScenario(membership, "character");

        var command = new CreateLoreEntityCommand(
            CampaignId: _campaignId,
            EntityType: "character",
            Name: "Player Character",
            Description: "My character");

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.OwnershipType.Should().Be(OwnershipType.Player);
        result.OwnerId.Should().Be(_userId);

        _loreEntityRepositoryMock.Verify(
            x => x.AddAsync(It.IsAny<LoreEntity>(), It.IsAny<CancellationToken>()), 
            Times.Once);
    }

    [Fact]
    public async Task Handle_WhenMasterCreatesSharedEntity_ShouldCreateWithSharedOwnership()
    {
        // Arrange
        var membership = CreateMasterMembership();
        SetupSuccessScenario(membership, "location");

        var command = new CreateLoreEntityCommand(
            CampaignId: _campaignId,
            EntityType: "location",
            Name: "Shared Location",
            OwnershipType: OwnershipType.Shared);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.OwnershipType.Should().Be(OwnershipType.Shared);
    }

    [Fact]
    public async Task Handle_WithCustomVisibility_ShouldUseProvidedVisibility()
    {
        // Arrange
        var membership = CreateMasterMembership();
        SetupSuccessScenario(membership, "npc");

        var command = new CreateLoreEntityCommand(
            CampaignId: _campaignId,
            EntityType: "npc",
            Name: "Secret NPC",
            Visibility: VisibilityLevel.Private);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Visibility.Should().Be(VisibilityLevel.Private);
    }

    [Fact]
    public async Task Handle_WithAttributes_ShouldStoreAttributes()
    {
        // Arrange
        var membership = CreateMasterMembership();
        SetupSuccessScenario(membership, "character");

        var attributes = new Dictionary<string, object>
        {
            { "strength", 18 },
            { "class", "Fighter" }
        };

        var command = new CreateLoreEntityCommand(
            CampaignId: _campaignId,
            EntityType: "character",
            Name: "Strong Character",
            Attributes: attributes);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Attributes.Should().NotBeNull();
    }

    [Fact]
    public async Task Handle_AsTemplate_ShouldCreateTemplate()
    {
        // Arrange
        var membership = CreateMasterMembership();
        SetupSuccessScenario(membership, "creature");

        var command = new CreateLoreEntityCommand(
            CampaignId: _campaignId,
            EntityType: "creature",
            Name: "Goblin Template",
            IsTemplate: true);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsTemplate.Should().BeTrue();
    }

    [Theory]
    [InlineData("character")]
    [InlineData("npc")]
    [InlineData("location")]
    [InlineData("item")]
    [InlineData("faction")]
    public async Task Handle_WithDifferentEntityTypes_ShouldCreateCorrectType(string entityType)
    {
        // Arrange
        var membership = CreateMasterMembership();
        SetupSuccessScenario(membership, entityType);

        var command = new CreateLoreEntityCommand(
            CampaignId: _campaignId,
            EntityType: entityType,
            Name: $"Test {entityType}");

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.EntityType.Should().Be(entityType);
    }

    #endregion

    #region Authentication and Authorization Failures

    [Fact]
    public async Task Handle_WhenNotAuthenticated_ShouldThrowForbiddenAccessException()
    {
        // Arrange
        _currentUserServiceMock.Setup(x => x.UserId).Returns((Guid?)null);

        var command = new CreateLoreEntityCommand(
            CampaignId: _campaignId,
            EntityType: "character",
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

        var command = new CreateLoreEntityCommand(
            CampaignId: _campaignId,
            EntityType: "character",
            Name: "Test");

        // Act
        var act = () => _handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<NotFoundException>()
            .WithMessage("*Campaign*");
    }

    [Fact]
    public async Task Handle_WhenPlayerTriesMasterOwnership_ShouldThrowForbiddenAccessException()
    {
        // Arrange
        var membership = CreatePlayerMembership();
        SetupSuccessScenario(membership, "npc");

        var command = new CreateLoreEntityCommand(
            CampaignId: _campaignId,
            EntityType: "npc",
            Name: "NPC",
            OwnershipType: OwnershipType.Master);

        // Act
        var act = () => _handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ForbiddenAccessException>()
            .WithMessage("*Players can only create player-owned entities*");
    }

    [Fact]
    public async Task Handle_WhenPlayerTriesSharedOwnership_ShouldThrowForbiddenAccessException()
    {
        // Arrange
        var membership = CreatePlayerMembership();
        SetupSuccessScenario(membership, "location");

        var command = new CreateLoreEntityCommand(
            CampaignId: _campaignId,
            EntityType: "location",
            Name: "Shared Location",
            OwnershipType: OwnershipType.Shared);

        // Act
        var act = () => _handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ForbiddenAccessException>()
            .WithMessage("*Players can only create player-owned entities*");
    }

    #endregion

    #region Template Validation Failures (EPIC 4)

    [Fact]
    public async Task Handle_WhenNoConfirmedTemplateExists_ShouldThrowValidationException()
    {
        // Arrange
        var membership = CreateMasterMembership();
        SetupCampaignMembership(membership);
        SetupCampaign();
        SetupNoTemplate("unknown_type");

        var command = new CreateLoreEntityCommand(
            CampaignId: _campaignId,
            EntityType: "unknown_type",
            Name: "Test Entity");

        // Act
        var act = () => _handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ValidationException>()
            .WithMessage("*not available*confirmed template*");
    }

    [Fact]
    public async Task Handle_WhenCampaignNotFound_ShouldThrowNotFoundException()
    {
        // Arrange
        var membership = CreateMasterMembership();
        SetupCampaignMembership(membership);
        _campaignRepositoryMock
            .Setup(x => x.GetByIdAsync(_campaignId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Campaign?)null);

        var command = new CreateLoreEntityCommand(
            CampaignId: _campaignId,
            EntityType: "character",
            Name: "Test");

        // Act
        var act = () => _handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<NotFoundException>()
            .WithMessage("*Campaign*");
    }

    [Fact]
    public async Task Handle_WhenEntityTypeNormalized_ShouldUseNormalizedName()
    {
        // Arrange
        var membership = CreateMasterMembership();
        SetupCampaignMembership(membership);
        SetupCampaign();
        
        // Template is for "player_character" (normalized)
        var template = CreateConfirmedTemplate("Player Character");
        _entityTemplateRepositoryMock
            .Setup(x => x.GetConfirmedTemplateForEntityTypeAsync(
                _gameSystemId, 
                _campaignOwnerId, 
                "player_character", // Normalized name
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(template);

        var command = new CreateLoreEntityCommand(
            CampaignId: _campaignId,
            EntityType: "Player Character", // Not normalized
            Name: "Test Character");

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.EntityType.Should().Be("player_character"); // Should use normalized name
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

    private void SetupCampaignMembership(CampaignMember membership)
    {
        _campaignMemberRepositoryMock
            .Setup(x => x.GetByCampaignAndUserAsync(_campaignId, _userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(membership);
    }

    private Campaign CreateCampaign()
    {
        return Campaign.Create(_campaignOwnerId, _gameSystemId, "Test Campaign");
    }

    private void SetupCampaign()
    {
        var campaign = CreateCampaign();
        _campaignRepositoryMock
            .Setup(x => x.GetByIdAsync(_campaignId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(campaign);
    }

    private EntityTemplate CreateConfirmedTemplate(string entityTypeName)
    {
        var template = EntityTemplate.Create(
            entityTypeName,
            entityTypeName,
            _gameSystemId,
            _campaignOwnerId);
        template.SetFieldDefinitions(new[] { FieldDefinition.Text("name", "Name") });
        template.Confirm(_campaignOwnerId);
        return template;
    }

    private void SetupConfirmedTemplate(string entityTypeName)
    {
        var normalizedName = EntityTemplate.NormalizeEntityTypeName(entityTypeName);
        var template = CreateConfirmedTemplate(entityTypeName);
        _entityTemplateRepositoryMock
            .Setup(x => x.GetConfirmedTemplateForEntityTypeAsync(
                _gameSystemId, 
                _campaignOwnerId, 
                normalizedName, 
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(template);
    }

    private void SetupNoTemplate(string entityTypeName)
    {
        var normalizedName = EntityTemplate.NormalizeEntityTypeName(entityTypeName);
        _entityTemplateRepositoryMock
            .Setup(x => x.GetConfirmedTemplateForEntityTypeAsync(
                _gameSystemId, 
                _campaignOwnerId, 
                normalizedName, 
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((EntityTemplate?)null);
    }

    /// <summary>
    /// Sets up all required mocks for a successful entity creation scenario.
    /// </summary>
    private void SetupSuccessScenario(CampaignMember membership, string entityType)
    {
        SetupCampaignMembership(membership);
        SetupCampaign();
        SetupConfirmedTemplate(entityType);
    }

    #endregion
}
