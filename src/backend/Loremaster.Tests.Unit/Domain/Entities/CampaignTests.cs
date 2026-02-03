using System.Text.Json;
using Loremaster.Domain.Entities;

namespace Loremaster.Tests.Unit.Domain.Entities;

/// <summary>
/// Unit tests for the Campaign domain entity.
/// Tests state transitions, join code generation, and lifecycle (EPIC 2 - Campaign Lifecycle).
/// </summary>
public class CampaignTests
{
    private readonly Guid _ownerId = Guid.NewGuid();
    private readonly Guid _gameSystemId = Guid.NewGuid();

    #region Create Tests

    [Fact]
    public void Create_WithValidData_ShouldCreateCampaign()
    {
        // Arrange & Act
        var campaign = Campaign.Create(
            ownerId: _ownerId,
            gameSystemId: _gameSystemId,
            name: "Test Campaign",
            description: "A test campaign description");

        // Assert
        campaign.OwnerId.Should().Be(_ownerId);
        campaign.GameSystemId.Should().Be(_gameSystemId);
        campaign.Name.Should().Be("Test Campaign");
        campaign.Description.Should().Be("A test campaign description");
        campaign.IsActive.Should().BeTrue(); // Default
        campaign.JoinCode.Should().NotBeNullOrEmpty();
        campaign.JoinCode.Should().HaveLength(8);
        campaign.Settings.Should().BeNull();
    }

    [Fact]
    public void Create_ShouldTrimName()
    {
        // Arrange & Act
        var campaign = Campaign.Create(
            _ownerId, _gameSystemId,
            name: "  Campaign Name  ");

        // Assert
        campaign.Name.Should().Be("Campaign Name");
    }

    [Fact]
    public void Create_ShouldTrimDescription()
    {
        // Arrange & Act
        var campaign = Campaign.Create(
            _ownerId, _gameSystemId,
            name: "Test",
            description: "  Some description  ");

        // Assert
        campaign.Description.Should().Be("Some description");
    }

    [Fact]
    public void Create_WithNullDescription_ShouldAllowNull()
    {
        // Arrange & Act
        var campaign = Campaign.Create(
            _ownerId, _gameSystemId,
            name: "Test",
            description: null);

        // Assert
        campaign.Description.Should().BeNull();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithInvalidName_ShouldThrowArgumentException(string? invalidName)
    {
        // Act
        var act = () => Campaign.Create(
            _ownerId, _gameSystemId,
            name: invalidName!);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithParameterName("name");
    }

    [Fact]
    public void Create_WithSettings_ShouldSetSettings()
    {
        // Arrange
        var settings = JsonDocument.Parse("{\"era\": \"Clone Wars\", \"houseRules\": true}");

        // Act
        var campaign = Campaign.Create(
            _ownerId, _gameSystemId,
            name: "Star Wars Campaign",
            settings: settings);

        // Assert
        campaign.Settings.Should().NotBeNull();
        campaign.Settings!.RootElement.GetProperty("era").GetString().Should().Be("Clone Wars");
    }

    [Fact]
    public void Create_ShouldGenerateUniqueJoinCodes()
    {
        // Act - Create multiple campaigns
        var campaigns = Enumerable.Range(0, 100)
            .Select(_ => Campaign.Create(_ownerId, _gameSystemId, "Test"))
            .ToList();

        // Assert - All join codes should be unique
        var joinCodes = campaigns.Select(c => c.JoinCode).ToList();
        joinCodes.Should().OnlyHaveUniqueItems();
    }

    [Fact]
    public void Create_JoinCode_ShouldBeUppercase()
    {
        // Act
        var campaign = Campaign.Create(_ownerId, _gameSystemId, "Test");

        // Assert
        campaign.JoinCode.Should().MatchRegex("^[A-Z0-9]{8}$");
    }

    #endregion

    #region Update Tests

    [Fact]
    public void Update_WithValidData_ShouldUpdateProperties()
    {
        // Arrange
        var campaign = Campaign.Create(_ownerId, _gameSystemId, "Original Name");

        // Act
        campaign.Update(
            name: "Updated Name",
            description: "New description");

        // Assert
        campaign.Name.Should().Be("Updated Name");
        campaign.Description.Should().Be("New description");
    }

    [Fact]
    public void Update_ShouldTrimValues()
    {
        // Arrange
        var campaign = Campaign.Create(_ownerId, _gameSystemId, "Original");

        // Act
        campaign.Update(
            name: "  Updated  ",
            description: "  Description  ");

        // Assert
        campaign.Name.Should().Be("Updated");
        campaign.Description.Should().Be("Description");
    }

    [Fact]
    public void Update_WithNullDescription_ShouldSetToNull()
    {
        // Arrange
        var campaign = Campaign.Create(
            _ownerId, _gameSystemId,
            name: "Test",
            description: "Original description");

        // Act
        campaign.Update(name: "Test", description: null);

        // Assert
        campaign.Description.Should().BeNull();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Update_WithInvalidName_ShouldThrowArgumentException(string? invalidName)
    {
        // Arrange
        var campaign = Campaign.Create(_ownerId, _gameSystemId, "Original");

        // Act
        var act = () => campaign.Update(name: invalidName!);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithParameterName("name");
    }

    [Fact]
    public void Update_WithSettings_ShouldReplaceSettings()
    {
        // Arrange
        var originalSettings = JsonDocument.Parse("{\"old\": true}");
        var campaign = Campaign.Create(
            _ownerId, _gameSystemId, "Test",
            settings: originalSettings);

        var newSettings = JsonDocument.Parse("{\"new\": true}");

        // Act
        campaign.Update(name: "Test", settings: newSettings);

        // Assert
        campaign.Settings.Should().NotBeNull();
        campaign.Settings!.RootElement.GetProperty("new").GetBoolean().Should().BeTrue();
    }

    [Fact]
    public void Update_WithNullSettings_ShouldNotChangeSettings()
    {
        // Arrange
        var settings = JsonDocument.Parse("{\"preserved\": true}");
        var campaign = Campaign.Create(
            _ownerId, _gameSystemId, "Test",
            settings: settings);

        // Act
        campaign.Update(name: "Updated", settings: null);

        // Assert - Settings should remain unchanged
        campaign.Settings.Should().NotBeNull();
        campaign.Settings!.RootElement.GetProperty("preserved").GetBoolean().Should().BeTrue();
    }

    #endregion

    #region JoinCode Tests

    [Fact]
    public void RegenerateJoinCode_ShouldCreateNewCode()
    {
        // Arrange
        var campaign = Campaign.Create(_ownerId, _gameSystemId, "Test");
        var originalCode = campaign.JoinCode;

        // Act
        campaign.RegenerateJoinCode();

        // Assert
        campaign.JoinCode.Should().NotBe(originalCode);
        campaign.JoinCode.Should().HaveLength(8);
    }

    [Fact]
    public void RegenerateJoinCode_MultipleCalls_ShouldGenerateUniqueCodes()
    {
        // Arrange
        var campaign = Campaign.Create(_ownerId, _gameSystemId, "Test");
        var codes = new HashSet<string> { campaign.JoinCode };

        // Act - Regenerate multiple times
        for (int i = 0; i < 50; i++)
        {
            campaign.RegenerateJoinCode();
            codes.Add(campaign.JoinCode);
        }

        // Assert - All codes should be unique (high probability with 8-char hex)
        codes.Should().HaveCount(51);
    }

    [Fact]
    public void JoinCode_ShouldBe8CharactersUppercaseAlphanumeric()
    {
        // Arrange
        var campaign = Campaign.Create(_ownerId, _gameSystemId, "Test");

        // Assert
        campaign.JoinCode.Should().HaveLength(8);
        campaign.JoinCode.Should().MatchRegex("^[A-Z0-9]+$");
    }

    #endregion

    #region Activation/Deactivation Tests

    [Fact]
    public void Create_ShouldBeActiveByDefault()
    {
        // Act
        var campaign = Campaign.Create(_ownerId, _gameSystemId, "Test");

        // Assert
        campaign.IsActive.Should().BeTrue();
    }

    [Fact]
    public void Deactivate_ShouldSetIsActiveToFalse()
    {
        // Arrange
        var campaign = Campaign.Create(_ownerId, _gameSystemId, "Test");

        // Act
        campaign.Deactivate();

        // Assert
        campaign.IsActive.Should().BeFalse();
    }

    [Fact]
    public void Activate_ShouldSetIsActiveToTrue()
    {
        // Arrange
        var campaign = Campaign.Create(_ownerId, _gameSystemId, "Test");
        campaign.Deactivate();

        // Act
        campaign.Activate();

        // Assert
        campaign.IsActive.Should().BeTrue();
    }

    [Fact]
    public void Deactivate_AlreadyInactive_ShouldRemainInactive()
    {
        // Arrange
        var campaign = Campaign.Create(_ownerId, _gameSystemId, "Test");
        campaign.Deactivate();

        // Act
        campaign.Deactivate();

        // Assert
        campaign.IsActive.Should().BeFalse();
    }

    [Fact]
    public void Activate_AlreadyActive_ShouldRemainActive()
    {
        // Arrange
        var campaign = Campaign.Create(_ownerId, _gameSystemId, "Test");

        // Act
        campaign.Activate();

        // Assert
        campaign.IsActive.Should().BeTrue();
    }

    #endregion

    #region State Transition Scenarios

    [Fact]
    public void Campaign_FullLifecycle_ShouldTrackStatesCorrectly()
    {
        // Create campaign
        var campaign = Campaign.Create(_ownerId, _gameSystemId, "Adventure Campaign");
        campaign.IsActive.Should().BeTrue();
        var originalJoinCode = campaign.JoinCode;

        // Update campaign
        campaign.Update("Epic Adventure", "A grand adventure awaits!");
        campaign.Name.Should().Be("Epic Adventure");
        campaign.Description.Should().Be("A grand adventure awaits!");
        campaign.JoinCode.Should().Be(originalJoinCode); // JoinCode unchanged

        // Deactivate for maintenance
        campaign.Deactivate();
        campaign.IsActive.Should().BeFalse();

        // Reactivate with new join code (for security)
        campaign.Activate();
        campaign.RegenerateJoinCode();
        campaign.IsActive.Should().BeTrue();
        campaign.JoinCode.Should().NotBe(originalJoinCode);
    }

    [Fact]
    public void Campaign_MultipleUpdates_ShouldMaintainConsistency()
    {
        // Arrange
        var campaign = Campaign.Create(_ownerId, _gameSystemId, "Test");

        // Act - Multiple updates
        campaign.Update("Update 1");
        campaign.Update("Update 2", "Description 2");
        campaign.Update("Final Name");

        // Assert
        campaign.Name.Should().Be("Final Name");
        campaign.Description.Should().BeNull(); // Null from last update
    }

    #endregion

    #region Edge Cases

    [Fact]
    public void Create_WithEmptyGuidOwnerId_ShouldStillCreate()
    {
        // Note: Domain doesn't validate Guid.Empty - that's a use case concern
        var campaign = Campaign.Create(
            ownerId: Guid.Empty,
            gameSystemId: _gameSystemId,
            name: "Test");

        campaign.OwnerId.Should().Be(Guid.Empty);
    }

    [Fact]
    public void Create_WithEmptyGuidGameSystemId_ShouldStillCreate()
    {
        // Note: Domain doesn't validate Guid.Empty - that's a use case concern
        var campaign = Campaign.Create(
            ownerId: _ownerId,
            gameSystemId: Guid.Empty,
            name: "Test");

        campaign.GameSystemId.Should().Be(Guid.Empty);
    }

    [Fact]
    public void Create_WithVeryLongName_ShouldAcceptIt()
    {
        // Note: Max length validation is handled by validator/DB constraints
        var longName = new string('A', 500);

        var campaign = Campaign.Create(_ownerId, _gameSystemId, longName);

        campaign.Name.Should().HaveLength(500);
    }

    [Fact]
    public void Create_WithSpecialCharactersInName_ShouldAcceptIt()
    {
        // Campaign names can have special characters
        var specialName = "Test Campaign! @#$%^&*()";

        var campaign = Campaign.Create(_ownerId, _gameSystemId, specialName);

        campaign.Name.Should().Be(specialName);
    }

    [Fact]
    public void Create_WithUnicodeCharacters_ShouldAcceptIt()
    {
        // Campaign names can have unicode
        var unicodeName = "冒険キャンペーン 🎲";

        var campaign = Campaign.Create(_ownerId, _gameSystemId, unicodeName);

        campaign.Name.Should().Be(unicodeName);
    }

    #endregion
}
