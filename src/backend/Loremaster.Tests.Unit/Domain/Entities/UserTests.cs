using Loremaster.Domain.Entities;
using Loremaster.Domain.Enums;

namespace Loremaster.Tests.Unit.Domain.Entities;

public class UserTests
{
    #region Create Tests

    [Fact]
    public void Create_WithValidData_ShouldCreateUser()
    {
        // Arrange
        var email = "Test@Example.com";
        var passwordHash = "hashedPassword123";
        var displayName = "Test User";

        // Act
        var user = User.Create(email, passwordHash, displayName);

        // Assert
        user.Email.Should().Be("test@example.com"); // Should be lowercase
        user.PasswordHash.Should().Be(passwordHash);
        user.DisplayName.Should().Be(displayName);
        user.Role.Should().Be(UserRole.Player);
        user.IsActive.Should().BeTrue();
        user.LastLoginAt.Should().BeNull();
        user.RefreshToken.Should().BeNull();
    }

    [Fact]
    public void Create_WithAdminRole_ShouldCreateAdminUser()
    {
        // Arrange & Act
        var user = User.Create("admin@example.com", "hash", "Admin", UserRole.Admin);

        // Assert
        user.Role.Should().Be(UserRole.Admin);
        user.IsAdmin.Should().BeTrue();
    }

    [Fact]
    public void Create_WithMasterRole_ShouldCreateMasterUser()
    {
        // Arrange & Act
        var user = User.Create("master@example.com", "hash", "Master", UserRole.Master);

        // Assert
        user.Role.Should().Be(UserRole.Master);
        user.IsMaster.Should().BeTrue();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithInvalidEmail_ShouldThrowArgumentException(string? invalidEmail)
    {
        // Act
        var act = () => User.Create(invalidEmail!, "hash", "Display Name");

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithParameterName("email");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithInvalidPasswordHash_ShouldThrowArgumentException(string? invalidHash)
    {
        // Act
        var act = () => User.Create("test@example.com", invalidHash!, "Display Name");

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithParameterName("passwordHash");
    }

    [Fact]
    public void Create_ShouldTrimEmailAndDisplayName()
    {
        // Arrange & Act
        var user = User.Create("  test@example.com  ", "hash", "  Test User  ");

        // Assert
        user.Email.Should().Be("test@example.com");
        user.DisplayName.Should().Be("Test User");
    }

    [Fact]
    public void Create_WithNullDisplayName_ShouldAllowNull()
    {
        // Arrange & Act
        var user = User.Create("test@example.com", "hash", null);

        // Assert
        user.DisplayName.Should().BeNull();
    }

    #endregion

    #region Update Tests

    [Fact]
    public void Update_WithValidName_ShouldUpdateDisplayName()
    {
        // Arrange
        var user = User.Create("test@example.com", "hash", "Original Name");

        // Act
        user.Update("New Name");

        // Assert
        user.DisplayName.Should().Be("New Name");
    }

    [Fact]
    public void Update_WithNull_ShouldSetToNull()
    {
        // Arrange
        var user = User.Create("test@example.com", "hash", "Original Name");

        // Act
        user.Update(null);

        // Assert
        user.DisplayName.Should().BeNull();
    }

    [Fact]
    public void Update_WithAvatarUrl_ShouldUpdateBoth()
    {
        // Arrange
        var user = User.Create("test@example.com", "hash", "Original Name");

        // Act
        user.Update("New Name", "https://example.com/avatar.png");

        // Assert
        user.DisplayName.Should().Be("New Name");
        user.AvatarUrl.Should().Be("https://example.com/avatar.png");
    }

    #endregion

    #region UpdatePassword Tests

    [Fact]
    public void UpdatePassword_WithValidHash_ShouldUpdatePassword()
    {
        // Arrange
        var user = User.Create("test@example.com", "oldHash", "Test User");
        var newHash = "newHash";

        // Act
        user.UpdatePassword(newHash);

        // Assert
        user.PasswordHash.Should().Be(newHash);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void UpdatePassword_WithInvalidHash_ShouldThrowArgumentException(string? invalidHash)
    {
        // Arrange
        var user = User.Create("test@example.com", "oldHash", "Test User");

        // Act
        var act = () => user.UpdatePassword(invalidHash!);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithParameterName("newPasswordHash");
    }

    #endregion

    #region Role Tests

    [Fact]
    public void ChangeRole_ShouldUpdateRole()
    {
        // Arrange
        var user = User.Create("test@example.com", "hash", "Test User");

        // Act
        user.ChangeRole(UserRole.Admin);

        // Assert
        user.Role.Should().Be(UserRole.Admin);
        user.IsAdmin.Should().BeTrue();
    }

    [Fact]
    public void ChangeRole_ToMaster_ShouldUpdateRole()
    {
        // Arrange
        var user = User.Create("test@example.com", "hash", "Test User");

        // Act
        user.ChangeRole(UserRole.Master);

        // Assert
        user.Role.Should().Be(UserRole.Master);
        user.IsMaster.Should().BeTrue();
    }

    [Fact]
    public void IsAdmin_ForRegularUser_ShouldReturnFalse()
    {
        // Arrange
        var user = User.Create("test@example.com", "hash", "Test User");

        // Assert
        user.IsAdmin.Should().BeFalse();
    }

    [Fact]
    public void IsPlayer_ForNewUser_ShouldReturnTrue()
    {
        // Arrange
        var user = User.Create("test@example.com", "hash", "Test User");

        // Assert
        user.IsPlayer.Should().BeTrue();
    }

    #endregion

    #region RefreshToken Tests

    [Fact]
    public void SetRefreshToken_ShouldSetTokenAndExpiry()
    {
        // Arrange
        var user = User.Create("test@example.com", "hash", "Test User");
        var token = "refreshToken123";
        var expiry = DateTime.UtcNow.AddDays(7);

        // Act
        user.SetRefreshToken(token, expiry);

        // Assert
        user.RefreshToken.Should().Be(token);
        user.RefreshTokenExpiryTime.Should().Be(expiry);
    }

    [Fact]
    public void RevokeRefreshToken_ShouldClearTokenAndExpiry()
    {
        // Arrange
        var user = User.Create("test@example.com", "hash", "Test User");
        user.SetRefreshToken("token", DateTime.UtcNow.AddDays(7));

        // Act
        user.RevokeRefreshToken();

        // Assert
        user.RefreshToken.Should().BeNull();
        user.RefreshTokenExpiryTime.Should().BeNull();
    }

    #endregion

    #region Activity Tests

    [Fact]
    public void RecordLogin_ShouldSetLastLoginAt()
    {
        // Arrange
        var user = User.Create("test@example.com", "hash", "Test User");
        var beforeLogin = DateTime.UtcNow;

        // Act
        user.RecordLogin();

        // Assert
        user.LastLoginAt.Should().NotBeNull();
        user.LastLoginAt.Should().BeOnOrAfter(beforeLogin);
        user.LastLoginAt.Should().BeOnOrBefore(DateTime.UtcNow);
    }

    [Fact]
    public void Deactivate_ShouldSetIsActiveToFalse()
    {
        // Arrange
        var user = User.Create("test@example.com", "hash", "Test User");

        // Act
        user.Deactivate();

        // Assert
        user.IsActive.Should().BeFalse();
    }

    [Fact]
    public void Deactivate_ShouldRevokeRefreshToken()
    {
        // Arrange
        var user = User.Create("test@example.com", "hash", "Test User");
        user.SetRefreshToken("token", DateTime.UtcNow.AddDays(7));

        // Act
        user.Deactivate();

        // Assert
        user.IsActive.Should().BeFalse();
        user.RefreshToken.Should().BeNull();
    }

    [Fact]
    public void Activate_ShouldSetIsActiveToTrue()
    {
        // Arrange
        var user = User.Create("test@example.com", "hash", "Test User");
        user.Deactivate();

        // Act
        user.Activate();

        // Assert
        user.IsActive.Should().BeTrue();
    }

    #endregion

    #region Master-Player Relationship Tests

    [Fact]
    public void Create_WithMasterId_ShouldSetMasterId()
    {
        // Arrange
        var masterId = Guid.NewGuid();

        // Act
        var player = User.Create("player@example.com", "hash", "Player", UserRole.Player, masterId);

        // Assert
        player.MasterId.Should().Be(masterId);
    }

    #endregion
}
