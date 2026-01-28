using Loremaster.Domain.Entities;
using Loremaster.Domain.Enums;

namespace Loremaster.Tests.Unit.Domain.Entities;

public class ProjectTests
{
    private readonly Guid _ownerId = Guid.NewGuid();

    #region Create Tests

    [Fact]
    public void Create_WithValidData_ShouldCreateProject()
    {
        // Arrange
        var name = "My Project";
        var description = "Project description";

        // Act
        var project = Project.Create(name, _ownerId, description);

        // Assert
        project.Name.Should().Be(name);
        project.Description.Should().Be(description);
        project.OwnerId.Should().Be(_ownerId);
        project.Status.Should().Be(ProjectStatus.Active);
        project.ArchivedAt.Should().BeNull();
    }

    [Fact]
    public void Create_WithoutDescription_ShouldCreateProjectWithNullDescription()
    {
        // Act
        var project = Project.Create("My Project", _ownerId);

        // Assert
        project.Description.Should().BeNull();
    }

    [Fact]
    public void Create_ShouldTrimNameAndDescription()
    {
        // Act
        var project = Project.Create("  My Project  ", _ownerId, "  Description  ");

        // Assert
        project.Name.Should().Be("My Project");
        project.Description.Should().Be("Description");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithInvalidName_ShouldThrowArgumentException(string? invalidName)
    {
        // Act
        var act = () => Project.Create(invalidName!, _ownerId);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithParameterName("name");
    }

    [Fact]
    public void Create_WithNameExceeding200Characters_ShouldThrowArgumentException()
    {
        // Arrange
        var longName = new string('a', 201);

        // Act
        var act = () => Project.Create(longName, _ownerId);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithParameterName("name");
    }

    [Fact]
    public void Create_WithNameExactly200Characters_ShouldSucceed()
    {
        // Arrange
        var name = new string('a', 200);

        // Act
        var project = Project.Create(name, _ownerId);

        // Assert
        project.Name.Should().HaveLength(200);
    }

    #endregion

    #region UpdateDetails Tests

    [Fact]
    public void UpdateDetails_WithValidData_ShouldUpdateNameAndDescription()
    {
        // Arrange
        var project = Project.Create("Original Name", _ownerId, "Original Description");

        // Act
        project.UpdateDetails("New Name", "New Description");

        // Assert
        project.Name.Should().Be("New Name");
        project.Description.Should().Be("New Description");
    }

    [Fact]
    public void UpdateDetails_WithNullDescription_ShouldSetDescriptionToNull()
    {
        // Arrange
        var project = Project.Create("Name", _ownerId, "Description");

        // Act
        project.UpdateDetails("Name", null);

        // Assert
        project.Description.Should().BeNull();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void UpdateDetails_WithInvalidName_ShouldThrowArgumentException(string? invalidName)
    {
        // Arrange
        var project = Project.Create("Name", _ownerId);

        // Act
        var act = () => project.UpdateDetails(invalidName!, "Description");

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithParameterName("name");
    }

    [Fact]
    public void UpdateDetails_WithNameExceeding200Characters_ShouldThrowArgumentException()
    {
        // Arrange
        var project = Project.Create("Name", _ownerId);
        var longName = new string('a', 201);

        // Act
        var act = () => project.UpdateDetails(longName, "Description");

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithParameterName("name");
    }

    #endregion

    #region Archive/Restore Tests

    [Fact]
    public void Archive_ShouldSetStatusToArchivedAndSetArchivedAt()
    {
        // Arrange
        var project = Project.Create("Name", _ownerId);
        var beforeArchive = DateTime.UtcNow;

        // Act
        project.Archive();

        // Assert
        project.Status.Should().Be(ProjectStatus.Archived);
        project.ArchivedAt.Should().NotBeNull();
        project.ArchivedAt.Should().BeOnOrAfter(beforeArchive);
    }

    [Fact]
    public void Archive_WhenAlreadyArchived_ShouldNotChangeArchivedAt()
    {
        // Arrange
        var project = Project.Create("Name", _ownerId);
        project.Archive();
        var originalArchivedAt = project.ArchivedAt;

        // Act
        project.Archive();

        // Assert
        project.ArchivedAt.Should().Be(originalArchivedAt);
    }

    [Fact]
    public void Restore_ShouldSetStatusToActiveAndClearArchivedAt()
    {
        // Arrange
        var project = Project.Create("Name", _ownerId);
        project.Archive();

        // Act
        project.Restore();

        // Assert
        project.Status.Should().Be(ProjectStatus.Active);
        project.ArchivedAt.Should().BeNull();
    }

    [Fact]
    public void Restore_WhenNotArchived_ShouldNotChangeStatus()
    {
        // Arrange
        var project = Project.Create("Name", _ownerId);

        // Act
        project.Restore();

        // Assert
        project.Status.Should().Be(ProjectStatus.Active);
        project.ArchivedAt.Should().BeNull();
    }

    #endregion

    #region Ownership Tests

    [Fact]
    public void IsOwnedBy_WithCorrectOwner_ShouldReturnTrue()
    {
        // Arrange
        var project = Project.Create("Name", _ownerId);

        // Act & Assert
        project.IsOwnedBy(_ownerId).Should().BeTrue();
    }

    [Fact]
    public void IsOwnedBy_WithDifferentUser_ShouldReturnFalse()
    {
        // Arrange
        var project = Project.Create("Name", _ownerId);
        var differentUserId = Guid.NewGuid();

        // Act & Assert
        project.IsOwnedBy(differentUserId).Should().BeFalse();
    }

    #endregion
}
