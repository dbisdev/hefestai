using Loremaster.Domain.Entities;

namespace Loremaster.Tests.Unit.Domain.Entities;

/// <summary>
/// Unit tests for GameSystem entity.
/// Tests game system creation, updates, and activation/deactivation.
/// </summary>
public class GameSystemTests
{
    #region Create Tests

    [Fact]
    public void Create_WithValidData_ShouldCreateGameSystem()
    {
        // Arrange & Act
        var gameSystem = GameSystem.Create(
            code: "DND5E",
            name: "Dungeons & Dragons 5th Edition",
            publisher: "Wizards of the Coast",
            version: "5.0");

        // Assert
        gameSystem.Should().NotBeNull();
        gameSystem.Code.Should().Be("dnd5e"); // Lowercased
        gameSystem.Name.Should().Be("Dungeons & Dragons 5th Edition");
        gameSystem.Publisher.Should().Be("Wizards of the Coast");
        gameSystem.Version.Should().Be("5.0");
        gameSystem.IsActive.Should().BeTrue();
    }

    [Fact]
    public void Create_ShouldNormalizeCodeToLowercase()
    {
        // Arrange & Act
        var gameSystem = GameSystem.Create(
            code: "PATHFINDER",
            name: "Pathfinder");

        // Assert
        gameSystem.Code.Should().Be("pathfinder");
    }

    [Fact]
    public void Create_ShouldTrimWhitespace()
    {
        // Arrange & Act
        var gameSystem = GameSystem.Create(
            code: "  SW5E  ",
            name: "  Star Wars 5E  ",
            publisher: "  SW5E Team  ");

        // Assert
        gameSystem.Code.Should().Be("sw5e");
        gameSystem.Name.Should().Be("Star Wars 5E");
        gameSystem.Publisher.Should().Be("SW5E Team");
    }

    [Fact]
    public void Create_WithSupportedEntityTypes_ShouldSetTypes()
    {
        // Arrange
        var entityTypes = new List<string> { "character", "npc", "vehicle" };

        // Act
        var gameSystem = GameSystem.Create(
            code: "TEST",
            name: "Test System",
            supportedEntityTypes: entityTypes);

        // Assert
        gameSystem.SupportedEntityTypes.Should().BeEquivalentTo(entityTypes);
    }

    [Fact]
    public void Create_WithoutSupportedEntityTypes_ShouldHaveEmptyList()
    {
        // Arrange & Act
        var gameSystem = GameSystem.Create("TEST", "Test System");

        // Assert
        gameSystem.SupportedEntityTypes.Should().BeEmpty();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithInvalidCode_ShouldThrowArgumentException(string? invalidCode)
    {
        // Act
        var act = () => GameSystem.Create(invalidCode!, "Test System");

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithParameterName("code");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithInvalidName_ShouldThrowArgumentException(string? invalidName)
    {
        // Act
        var act = () => GameSystem.Create("TEST", invalidName!);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithParameterName("name");
    }

    #endregion

    #region Update Tests

    [Fact]
    public void Update_WithValidData_ShouldUpdateGameSystem()
    {
        // Arrange
        var gameSystem = GameSystem.Create("TEST", "Original Name");

        // Act
        gameSystem.Update(
            name: "Updated Name",
            publisher: "New Publisher",
            version: "2.0");

        // Assert
        gameSystem.Name.Should().Be("Updated Name");
        gameSystem.Publisher.Should().Be("New Publisher");
        gameSystem.Version.Should().Be("2.0");
    }

    [Fact]
    public void Update_ShouldTrimWhitespace()
    {
        // Arrange
        var gameSystem = GameSystem.Create("TEST", "Original");

        // Act
        gameSystem.Update("  Updated Name  ", "  Publisher  ");

        // Assert
        gameSystem.Name.Should().Be("Updated Name");
        gameSystem.Publisher.Should().Be("Publisher");
    }

    [Fact]
    public void Update_WithSupportedEntityTypes_ShouldUpdateTypes()
    {
        // Arrange
        var gameSystem = GameSystem.Create("TEST", "Test");
        var newTypes = new List<string> { "character", "ship" };

        // Act
        gameSystem.Update("Test", supportedEntityTypes: newTypes);

        // Assert
        gameSystem.SupportedEntityTypes.Should().BeEquivalentTo(newTypes);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Update_WithInvalidName_ShouldThrowArgumentException(string? invalidName)
    {
        // Arrange
        var gameSystem = GameSystem.Create("TEST", "Test System");

        // Act
        var act = () => gameSystem.Update(invalidName!);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithParameterName("name");
    }

    #endregion

    #region Activation Tests

    [Fact]
    public void Activate_ShouldSetActiveTrue()
    {
        // Arrange
        var gameSystem = GameSystem.Create("TEST", "Test");
        gameSystem.Deactivate();

        // Act
        gameSystem.Activate();

        // Assert
        gameSystem.IsActive.Should().BeTrue();
    }

    [Fact]
    public void Deactivate_ShouldSetActiveFalse()
    {
        // Arrange
        var gameSystem = GameSystem.Create("TEST", "Test");

        // Act
        gameSystem.Deactivate();

        // Assert
        gameSystem.IsActive.Should().BeFalse();
    }

    #endregion
}
