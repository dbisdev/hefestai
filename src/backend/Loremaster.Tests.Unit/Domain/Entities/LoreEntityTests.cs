using System.Text.Json;
using Loremaster.Domain.Entities;
using Loremaster.Domain.Enums;

namespace Loremaster.Tests.Unit.Domain.Entities;

/// <summary>
/// Unit tests for the LoreEntity domain entity.
/// Tests ownership rules, visibility matrix, and permission logic (EPIC 1 - Core Ownership & Permissions).
/// </summary>
public class LoreEntityTests
{
    private readonly Guid _campaignId = Guid.NewGuid();
    private readonly Guid _ownerId = Guid.NewGuid();
    private readonly Guid _otherUserId = Guid.NewGuid();

    #region Create Tests

    [Fact]
    public void Create_WithValidData_ShouldCreateEntity()
    {
        // Arrange & Act
        var entity = LoreEntity.Create(
            campaignId: _campaignId,
            ownerId: _ownerId,
            entityType: "Character",
            name: "Test Character",
            description: "A test character description");

        // Assert
        entity.CampaignId.Should().Be(_campaignId);
        entity.OwnerId.Should().Be(_ownerId);
        entity.EntityType.Should().Be("character"); // Should be lowercase
        entity.Name.Should().Be("Test Character");
        entity.Description.Should().Be("A test character description");
        entity.OwnershipType.Should().Be(OwnershipType.Master); // Default
        entity.Visibility.Should().Be(VisibilityLevel.Campaign); // Default
        entity.IsTemplate.Should().BeFalse();
        entity.ImageUrl.Should().BeNull();
        entity.Attributes.Should().BeNull();
        entity.Metadata.Should().BeNull();
    }

    [Fact]
    public void Create_ShouldTrimAndNormalizeEntityType()
    {
        // Arrange & Act
        var entity = LoreEntity.Create(
            campaignId: _campaignId,
            ownerId: _ownerId,
            entityType: "  CHARACTER  ",
            name: "Test");

        // Assert
        entity.EntityType.Should().Be("character");
    }

    [Fact]
    public void Create_ShouldTrimName()
    {
        // Arrange & Act
        var entity = LoreEntity.Create(
            campaignId: _campaignId,
            ownerId: _ownerId,
            entityType: "npc",
            name: "  Test Name  ");

        // Assert
        entity.Name.Should().Be("Test Name");
    }

    [Fact]
    public void Create_ShouldTrimDescription()
    {
        // Arrange & Act
        var entity = LoreEntity.Create(
            campaignId: _campaignId,
            ownerId: _ownerId,
            entityType: "npc",
            name: "Test",
            description: "  Some description  ");

        // Assert
        entity.Description.Should().Be("Some description");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithInvalidEntityType_ShouldThrowArgumentException(string? invalidType)
    {
        // Act
        var act = () => LoreEntity.Create(
            campaignId: _campaignId,
            ownerId: _ownerId,
            entityType: invalidType!,
            name: "Test");

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithParameterName("entityType");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithInvalidName_ShouldThrowArgumentException(string? invalidName)
    {
        // Act
        var act = () => LoreEntity.Create(
            campaignId: _campaignId,
            ownerId: _ownerId,
            entityType: "character",
            name: invalidName!);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithParameterName("name");
    }

    [Theory]
    [InlineData(OwnershipType.Master)]
    [InlineData(OwnershipType.Player)]
    [InlineData(OwnershipType.Shared)]
    public void Create_WithOwnershipType_ShouldSetOwnershipType(OwnershipType ownershipType)
    {
        // Arrange & Act
        var entity = LoreEntity.Create(
            campaignId: _campaignId,
            ownerId: _ownerId,
            entityType: "character",
            name: "Test",
            ownershipType: ownershipType);

        // Assert
        entity.OwnershipType.Should().Be(ownershipType);
    }

    [Theory]
    [InlineData(VisibilityLevel.Draft)]
    [InlineData(VisibilityLevel.Private)]
    [InlineData(VisibilityLevel.Campaign)]
    [InlineData(VisibilityLevel.Public)]
    public void Create_WithVisibilityLevel_ShouldSetVisibility(VisibilityLevel visibility)
    {
        // Arrange & Act
        var entity = LoreEntity.Create(
            campaignId: _campaignId,
            ownerId: _ownerId,
            entityType: "character",
            name: "Test",
            visibility: visibility);

        // Assert
        entity.Visibility.Should().Be(visibility);
    }

    [Fact]
    public void Create_WithAllParameters_ShouldSetAllProperties()
    {
        // Arrange
        var generationRequestId = Guid.NewGuid();
        var attributes = JsonDocument.Parse("{\"strength\": 10}");
        var metadata = JsonDocument.Parse("{\"tags\": [\"hero\"]}");

        // Act
        var entity = LoreEntity.Create(
            campaignId: _campaignId,
            ownerId: _ownerId,
            entityType: "character",
            name: "Hero",
            description: "A brave hero",
            ownershipType: OwnershipType.Player,
            visibility: VisibilityLevel.Campaign,
            isTemplate: true,
            imageUrl: "https://example.com/image.png",
            attributes: attributes,
            metadata: metadata,
            generationRequestId: generationRequestId);

        // Assert
        entity.GenerationRequestId.Should().Be(generationRequestId);
        entity.IsTemplate.Should().BeTrue();
        entity.ImageUrl.Should().Be("https://example.com/image.png");
        entity.Attributes.Should().NotBeNull();
        entity.Metadata.Should().NotBeNull();
    }

    #endregion

    #region CreatePlayerCharacter Tests

    [Fact]
    public void CreatePlayerCharacter_ShouldCreateWithCorrectDefaults()
    {
        // Arrange
        var playerId = Guid.NewGuid();

        // Act
        var character = LoreEntity.CreatePlayerCharacter(
            campaignId: _campaignId,
            playerId: playerId,
            name: "Player Character");

        // Assert
        character.OwnerId.Should().Be(playerId);
        character.EntityType.Should().Be("character");
        character.OwnershipType.Should().Be(OwnershipType.Player);
        character.Visibility.Should().Be(VisibilityLevel.Draft); // Default for player characters
        character.IsPlayerCharacter.Should().BeTrue();
    }

    [Fact]
    public void CreatePlayerCharacter_WithCustomVisibility_ShouldUseProvidedVisibility()
    {
        // Arrange & Act
        var character = LoreEntity.CreatePlayerCharacter(
            campaignId: _campaignId,
            playerId: _ownerId,
            name: "PC",
            visibility: VisibilityLevel.Campaign);

        // Assert
        character.Visibility.Should().Be(VisibilityLevel.Campaign);
    }

    [Fact]
    public void IsPlayerCharacter_ForPlayerOwnedCharacter_ShouldReturnTrue()
    {
        // Arrange
        var entity = LoreEntity.Create(
            campaignId: _campaignId,
            ownerId: _ownerId,
            entityType: "character",
            name: "Test",
            ownershipType: OwnershipType.Player);

        // Assert
        entity.IsPlayerCharacter.Should().BeTrue();
    }

    [Fact]
    public void IsPlayerCharacter_ForMasterOwnedCharacter_ShouldReturnFalse()
    {
        // Arrange
        var entity = LoreEntity.Create(
            campaignId: _campaignId,
            ownerId: _ownerId,
            entityType: "character",
            name: "NPC",
            ownershipType: OwnershipType.Master);

        // Assert
        entity.IsPlayerCharacter.Should().BeFalse();
    }

    [Fact]
    public void IsPlayerCharacter_ForPlayerOwnedNonCharacter_ShouldReturnFalse()
    {
        // Arrange
        var entity = LoreEntity.Create(
            campaignId: _campaignId,
            ownerId: _ownerId,
            entityType: "vehicle",
            name: "Ship",
            ownershipType: OwnershipType.Player);

        // Assert
        entity.IsPlayerCharacter.Should().BeFalse();
    }

    #endregion

    #region Update Tests

    [Fact]
    public void Update_WithValidData_ShouldUpdateProperties()
    {
        // Arrange
        var entity = LoreEntity.Create(_campaignId, _ownerId, "character", "Original");

        // Act
        entity.Update(
            name: "Updated Name",
            description: "Updated description",
            visibility: VisibilityLevel.Public,
            imageUrl: "https://new-image.png");

        // Assert
        entity.Name.Should().Be("Updated Name");
        entity.Description.Should().Be("Updated description");
        entity.Visibility.Should().Be(VisibilityLevel.Public);
        entity.ImageUrl.Should().Be("https://new-image.png");
    }

    [Fact]
    public void Update_WithNullVisibility_ShouldNotChangeVisibility()
    {
        // Arrange
        var entity = LoreEntity.Create(
            _campaignId, _ownerId, "character", "Test",
            visibility: VisibilityLevel.Private);

        // Act
        entity.Update(name: "Updated", visibility: null);

        // Assert
        entity.Visibility.Should().Be(VisibilityLevel.Private);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Update_WithInvalidName_ShouldThrowArgumentException(string? invalidName)
    {
        // Arrange
        var entity = LoreEntity.Create(_campaignId, _ownerId, "character", "Original");

        // Act
        var act = () => entity.Update(name: invalidName!);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithParameterName("name");
    }

    [Fact]
    public void Update_WithAttributes_ShouldReplaceAttributes()
    {
        // Arrange
        var originalAttributes = JsonDocument.Parse("{\"old\": true}");
        var entity = LoreEntity.Create(
            _campaignId, _ownerId, "character", "Test",
            attributes: originalAttributes);
        
        var newAttributes = JsonDocument.Parse("{\"new\": true}");

        // Act
        entity.Update(name: "Test", attributes: newAttributes);

        // Assert
        entity.Attributes.Should().NotBeNull();
        entity.Attributes!.RootElement.GetProperty("new").GetBoolean().Should().BeTrue();
    }

    #endregion

    #region Visibility and Ownership Changes Tests

    [Fact]
    public void ChangeVisibility_ShouldUpdateVisibility()
    {
        // Arrange
        var entity = LoreEntity.Create(
            _campaignId, _ownerId, "character", "Test",
            visibility: VisibilityLevel.Draft);

        // Act
        entity.ChangeVisibility(VisibilityLevel.Campaign);

        // Assert
        entity.Visibility.Should().Be(VisibilityLevel.Campaign);
    }

    [Fact]
    public void ChangeOwnershipType_ShouldUpdateOwnershipType()
    {
        // Arrange
        var entity = LoreEntity.Create(
            _campaignId, _ownerId, "character", "Test",
            ownershipType: OwnershipType.Master);

        // Act
        entity.ChangeOwnershipType(OwnershipType.Shared);

        // Assert
        entity.OwnershipType.Should().Be(OwnershipType.Shared);
    }

    [Fact]
    public void TransferOwnership_ShouldChangeOwner()
    {
        // Arrange
        var entity = LoreEntity.Create(_campaignId, _ownerId, "character", "Test");
        var newOwnerId = Guid.NewGuid();

        // Act
        entity.TransferOwnership(newOwnerId);

        // Assert
        entity.OwnerId.Should().Be(newOwnerId);
    }

    [Fact]
    public void TransferOwnership_WithNewOwnershipType_ShouldChangeBoth()
    {
        // Arrange
        var entity = LoreEntity.Create(
            _campaignId, _ownerId, "character", "Test",
            ownershipType: OwnershipType.Master);
        var newOwnerId = Guid.NewGuid();

        // Act
        entity.TransferOwnership(newOwnerId, OwnershipType.Player);

        // Assert
        entity.OwnerId.Should().Be(newOwnerId);
        entity.OwnershipType.Should().Be(OwnershipType.Player);
    }

    [Fact]
    public void SetAsTemplate_ShouldSetIsTemplateTrue()
    {
        // Arrange
        var entity = LoreEntity.Create(_campaignId, _ownerId, "character", "Test");

        // Act
        entity.SetAsTemplate();

        // Assert
        entity.IsTemplate.Should().BeTrue();
    }

    [Fact]
    public void SetAsTemplate_WithFalse_ShouldSetIsTemplateFalse()
    {
        // Arrange
        var entity = LoreEntity.Create(
            _campaignId, _ownerId, "character", "Test",
            isTemplate: true);

        // Act
        entity.SetAsTemplate(false);

        // Assert
        entity.IsTemplate.Should().BeFalse();
    }

    [Fact]
    public void LinkToGenerationRequest_ShouldSetGenerationRequestId()
    {
        // Arrange
        var entity = LoreEntity.Create(_campaignId, _ownerId, "character", "Test");
        var requestId = Guid.NewGuid();

        // Act
        entity.LinkToGenerationRequest(requestId);

        // Assert
        entity.GenerationRequestId.Should().Be(requestId);
    }

    #endregion

    #region CanBeReadBy Tests - Visibility Matrix

    [Fact]
    public void CanBeReadBy_Owner_ShouldAlwaysReturnTrue()
    {
        // Test all visibility levels - owner should always be able to read
        var visibilityLevels = new[]
        {
            VisibilityLevel.Draft,
            VisibilityLevel.Private,
            VisibilityLevel.Campaign,
            VisibilityLevel.Public
        };

        foreach (var visibility in visibilityLevels)
        {
            // Arrange
            var entity = LoreEntity.Create(
                _campaignId, _ownerId, "character", "Test",
                visibility: visibility);

            // Act & Assert - Owner can always read, regardless of campaign membership
            entity.CanBeReadBy(_ownerId, isCampaignMember: false, isCampaignMaster: false)
                .Should().BeTrue($"Owner should read {visibility} entity");
            entity.CanBeReadBy(_ownerId, isCampaignMember: true, isCampaignMaster: false)
                .Should().BeTrue($"Owner should read {visibility} entity as member");
            entity.CanBeReadBy(_ownerId, isCampaignMember: true, isCampaignMaster: true)
                .Should().BeTrue($"Owner should read {visibility} entity as master");
        }
    }

    [Fact]
    public void CanBeReadBy_PublicVisibility_ShouldAllowAnyone()
    {
        // Arrange
        var entity = LoreEntity.Create(
            _campaignId, _ownerId, "character", "Test",
            visibility: VisibilityLevel.Public);

        // Act & Assert - Anyone can read public entities
        entity.CanBeReadBy(_otherUserId, isCampaignMember: false, isCampaignMaster: false)
            .Should().BeTrue("Non-member should read public entity");
        entity.CanBeReadBy(_otherUserId, isCampaignMember: true, isCampaignMaster: false)
            .Should().BeTrue("Member should read public entity");
        entity.CanBeReadBy(_otherUserId, isCampaignMember: true, isCampaignMaster: true)
            .Should().BeTrue("Master should read public entity");
    }

    [Fact]
    public void CanBeReadBy_CampaignVisibility_ShouldAllowMembersOnly()
    {
        // Arrange
        var entity = LoreEntity.Create(
            _campaignId, _ownerId, "character", "Test",
            visibility: VisibilityLevel.Campaign);

        // Act & Assert
        entity.CanBeReadBy(_otherUserId, isCampaignMember: false, isCampaignMaster: false)
            .Should().BeFalse("Non-member should NOT read campaign entity");
        entity.CanBeReadBy(_otherUserId, isCampaignMember: true, isCampaignMaster: false)
            .Should().BeTrue("Member should read campaign entity");
        entity.CanBeReadBy(_otherUserId, isCampaignMember: true, isCampaignMaster: true)
            .Should().BeTrue("Master should read campaign entity");
    }

    [Fact]
    public void CanBeReadBy_PrivateVisibility_ShouldAllowMasterOnly()
    {
        // Arrange
        var entity = LoreEntity.Create(
            _campaignId, _ownerId, "character", "Test",
            visibility: VisibilityLevel.Private);

        // Act & Assert
        entity.CanBeReadBy(_otherUserId, isCampaignMember: false, isCampaignMaster: false)
            .Should().BeFalse("Non-member should NOT read private entity");
        entity.CanBeReadBy(_otherUserId, isCampaignMember: true, isCampaignMaster: false)
            .Should().BeFalse("Member (non-master) should NOT read private entity");
        entity.CanBeReadBy(_otherUserId, isCampaignMember: true, isCampaignMaster: true)
            .Should().BeTrue("Master should read private entity");
    }

    [Fact]
    public void CanBeReadBy_DraftVisibility_ShouldAllowOwnerOnly()
    {
        // Arrange
        var entity = LoreEntity.Create(
            _campaignId, _ownerId, "character", "Test",
            visibility: VisibilityLevel.Draft);

        // Act & Assert - Only owner can read drafts
        entity.CanBeReadBy(_otherUserId, isCampaignMember: false, isCampaignMaster: false)
            .Should().BeFalse("Non-member should NOT read draft entity");
        entity.CanBeReadBy(_otherUserId, isCampaignMember: true, isCampaignMaster: false)
            .Should().BeFalse("Member should NOT read draft entity");
        entity.CanBeReadBy(_otherUserId, isCampaignMember: true, isCampaignMaster: true)
            .Should().BeFalse("Even master should NOT read draft entity (owner only)");
        
        // Owner can read
        entity.CanBeReadBy(_ownerId, isCampaignMember: true, isCampaignMaster: false)
            .Should().BeTrue("Owner should read own draft entity");
    }

    #endregion

    #region CanBeWrittenBy Tests - Ownership Rules

    [Fact]
    public void CanBeWrittenBy_Owner_ShouldAlwaysReturnTrue()
    {
        // Test all ownership types - owner should always be able to write
        var ownershipTypes = new[]
        {
            OwnershipType.Master,
            OwnershipType.Player,
            OwnershipType.Shared
        };

        foreach (var ownership in ownershipTypes)
        {
            // Arrange
            var entity = LoreEntity.Create(
                _campaignId, _ownerId, "character", "Test",
                ownershipType: ownership);

            // Act & Assert
            entity.CanBeWrittenBy(_ownerId, isCampaignMaster: false)
                .Should().BeTrue($"Owner should write {ownership} entity");
            entity.CanBeWrittenBy(_ownerId, isCampaignMaster: true)
                .Should().BeTrue($"Owner (as master) should write {ownership} entity");
        }
    }

    [Fact]
    public void CanBeWrittenBy_PlayerOwnedEntity_ShouldDenyNonOwner()
    {
        // Arrange - Player-owned entity (like a player's character)
        var entity = LoreEntity.Create(
            _campaignId, _ownerId, "character", "Player Character",
            ownershipType: OwnershipType.Player);

        // Act & Assert - Only the owning player can edit, not even the master
        entity.CanBeWrittenBy(_otherUserId, isCampaignMaster: false)
            .Should().BeFalse("Non-owner non-master should NOT write player entity");
        entity.CanBeWrittenBy(_otherUserId, isCampaignMaster: true)
            .Should().BeFalse("Even campaign master should NOT write player-owned entity");
    }

    [Fact]
    public void CanBeWrittenBy_MasterOwnedEntity_ShouldAllowCampaignMaster()
    {
        // Arrange - Master-owned entity (like an NPC)
        var entity = LoreEntity.Create(
            _campaignId, _ownerId, "npc", "NPC",
            ownershipType: OwnershipType.Master);

        // Act & Assert
        entity.CanBeWrittenBy(_otherUserId, isCampaignMaster: false)
            .Should().BeFalse("Non-master should NOT write master entity");
        entity.CanBeWrittenBy(_otherUserId, isCampaignMaster: true)
            .Should().BeTrue("Campaign master should write master entity");
    }

    [Fact]
    public void CanBeWrittenBy_SharedEntity_ShouldAllowCampaignMaster()
    {
        // Arrange - Shared entity (collaborative content)
        var entity = LoreEntity.Create(
            _campaignId, _ownerId, "location", "Tavern",
            ownershipType: OwnershipType.Shared);

        // Act & Assert
        entity.CanBeWrittenBy(_otherUserId, isCampaignMaster: false)
            .Should().BeFalse("Non-master should NOT write shared entity");
        entity.CanBeWrittenBy(_otherUserId, isCampaignMaster: true)
            .Should().BeTrue("Campaign master should write shared entity");
    }

    #endregion

    #region Combined Permission Scenarios

    [Fact]
    public void Permissions_PlayerCharacter_ShouldHaveCorrectAccess()
    {
        // Scenario: Player creates a character (default draft), publishes to campaign
        var playerId = Guid.NewGuid();
        var masterId = Guid.NewGuid();
        var otherPlayerId = Guid.NewGuid();

        // Draft character - only player can see/edit
        var draftCharacter = LoreEntity.CreatePlayerCharacter(
            _campaignId, playerId, "Draft PC");

        draftCharacter.CanBeReadBy(playerId, true, false).Should().BeTrue("Player reads own draft");
        draftCharacter.CanBeReadBy(masterId, true, true).Should().BeFalse("Master cannot read player draft");
        draftCharacter.CanBeWrittenBy(playerId, false).Should().BeTrue("Player edits own character");
        draftCharacter.CanBeWrittenBy(masterId, true).Should().BeFalse("Master cannot edit player character");

        // Published character - campaign can see, only player can edit
        draftCharacter.ChangeVisibility(VisibilityLevel.Campaign);

        draftCharacter.CanBeReadBy(playerId, true, false).Should().BeTrue("Player reads own published");
        draftCharacter.CanBeReadBy(masterId, true, true).Should().BeTrue("Master reads published");
        draftCharacter.CanBeReadBy(otherPlayerId, true, false).Should().BeTrue("Other player reads published");
        draftCharacter.CanBeWrittenBy(playerId, false).Should().BeTrue("Player still edits own");
        draftCharacter.CanBeWrittenBy(masterId, true).Should().BeFalse("Master still cannot edit player character");
    }

    [Fact]
    public void Permissions_MasterNPC_ShouldHaveCorrectAccess()
    {
        // Scenario: Master creates an NPC with private visibility
        var masterId = Guid.NewGuid();
        var playerId = Guid.NewGuid();

        var npc = LoreEntity.Create(
            _campaignId,
            masterId,
            "npc",
            "Secret Villain",
            ownershipType: OwnershipType.Master,
            visibility: VisibilityLevel.Private);

        // Private - only master can see
        npc.CanBeReadBy(masterId, true, true).Should().BeTrue("Master reads own NPC");
        npc.CanBeReadBy(playerId, true, false).Should().BeFalse("Player cannot read private NPC");
        npc.CanBeWrittenBy(masterId, true).Should().BeTrue("Master edits own NPC");

        // Reveal to campaign
        npc.ChangeVisibility(VisibilityLevel.Campaign);

        npc.CanBeReadBy(playerId, true, false).Should().BeTrue("Player can now read revealed NPC");
        npc.CanBeWrittenBy(playerId, false).Should().BeFalse("Player still cannot edit master NPC");
    }

    [Fact]
    public void Permissions_SharedLocation_ShouldHaveCorrectAccess()
    {
        // Scenario: Master creates a shared location that any master can edit
        var masterId = Guid.NewGuid();
        var coMasterId = Guid.NewGuid();
        var playerId = Guid.NewGuid();

        var location = LoreEntity.Create(
            _campaignId,
            masterId,
            "location",
            "The Tavern",
            ownershipType: OwnershipType.Shared,
            visibility: VisibilityLevel.Campaign);

        // All members can read
        location.CanBeReadBy(masterId, true, true).Should().BeTrue();
        location.CanBeReadBy(coMasterId, true, true).Should().BeTrue();
        location.CanBeReadBy(playerId, true, false).Should().BeTrue();

        // Only masters can edit shared content
        location.CanBeWrittenBy(masterId, true).Should().BeTrue("Owner master edits");
        location.CanBeWrittenBy(coMasterId, true).Should().BeTrue("Co-master edits shared");
        location.CanBeWrittenBy(playerId, false).Should().BeFalse("Player cannot edit shared");
    }

    #endregion

    #region Negative Tests - Invalid State Rejections

    [Fact]
    public void Create_WithEmptyGuidCampaignId_ShouldStillCreate()
    {
        // Note: Domain doesn't validate Guid.Empty - that's a use case concern
        // This test documents current behavior
        var entity = LoreEntity.Create(
            campaignId: Guid.Empty,
            ownerId: _ownerId,
            entityType: "character",
            name: "Test");

        entity.CampaignId.Should().Be(Guid.Empty);
    }

    [Fact]
    public void Create_WithEmptyGuidOwnerId_ShouldStillCreate()
    {
        // Note: Domain doesn't validate Guid.Empty - that's a use case concern
        var entity = LoreEntity.Create(
            campaignId: _campaignId,
            ownerId: Guid.Empty,
            entityType: "character",
            name: "Test");

        entity.OwnerId.Should().Be(Guid.Empty);
    }

    [Fact]
    public void TransferOwnership_ToEmptyGuid_ShouldStillTransfer()
    {
        // Note: Domain doesn't validate Guid.Empty - that's a use case concern
        var entity = LoreEntity.Create(_campaignId, _ownerId, "character", "Test");

        entity.TransferOwnership(Guid.Empty);

        entity.OwnerId.Should().Be(Guid.Empty);
    }

    #endregion
}
