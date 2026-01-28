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

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithInvalidEmail_ShouldThrowArgumentException(string? invalidEmail)
    {
        // Act
        var act = () => User.Create(invalidEmail!, "hash");

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
        var act = () => User.Create("test@example.com", invalidHash!);

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

    #endregion

    #region UpdateDisplayName Tests

    [Fact]
    public void UpdateDisplayName_WithValidName_ShouldUpdateDisplayName()
    {
        // Arrange
        var user = User.Create("test@example.com", "hash", "Original Name");

        // Act
        user.UpdateDisplayName("New Name");

        // Assert
        user.DisplayName.Should().Be("New Name");
    }

    [Fact]
    public void UpdateDisplayName_WithNull_ShouldSetToNull()
    {
        // Arrange
        var user = User.Create("test@example.com", "hash", "Original Name");

        // Act
        user.UpdateDisplayName(null);

        // Assert
        user.DisplayName.Should().BeNull();
    }

    #endregion

    #region UpdatePassword Tests

    [Fact]
    public void UpdatePassword_WithValidHash_ShouldUpdatePassword()
    {
        // Arrange
        var user = User.Create("test@example.com", "oldHash");
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
        var user = User.Create("test@example.com", "oldHash");

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
        var user = User.Create("test@example.com", "hash");

        // Act
        user.ChangeRole(UserRole.Admin);

        // Assert
        user.Role.Should().Be(UserRole.Admin);
        user.IsAdmin.Should().BeTrue();
    }

    [Fact]
    public void IsAdmin_ForRegularUser_ShouldReturnFalse()
    {
        // Arrange
        var user = User.Create("test@example.com", "hash");

        // Assert
        user.IsAdmin.Should().BeFalse();
    }

    #endregion

    #region RefreshToken Tests

    [Fact]
    public void SetRefreshToken_ShouldSetTokenAndExpiry()
    {
        // Arrange
        var user = User.Create("test@example.com", "hash");
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
        var user = User.Create("test@example.com", "hash");
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
        var user = User.Create("test@example.com", "hash");
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
        var user = User.Create("test@example.com", "hash");

        // Act
        user.Deactivate();

        // Assert
        user.IsActive.Should().BeFalse();
    }

    [Fact]
    public void Activate_ShouldSetIsActiveToTrue()
    {
        // Arrange
        var user = User.Create("test@example.com", "hash");
        user.Deactivate();

        // Act
        user.Activate();

        // Assert
        user.IsActive.Should().BeTrue();
    }

    #endregion
}
