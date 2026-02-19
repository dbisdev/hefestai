using Loremaster.Infrastructure.Identity;
using BCrypt.Net;

namespace Loremaster.Tests.Unit.Infrastructure.Identity;

/// <summary>
/// Unit tests for PasswordHasher.
/// Tests password hashing and verification.
/// </summary>
public class PasswordHasherTests
{
    private readonly PasswordHasher _passwordHasher;

    public PasswordHasherTests()
    {
        _passwordHasher = new PasswordHasher();
    }

    #region HashPassword Tests

    [Fact]
    public void HashPassword_WithValidPassword_ShouldReturnHash()
    {
        // Arrange
        var password = "MySecurePassword123!";

        // Act
        var hash = _passwordHasher.HashPassword(password);

        // Assert
        hash.Should().NotBeNullOrEmpty();
        hash.Should().NotBe(password);
    }

    [Fact]
    public void HashPassword_ShouldReturnUniqueHashesForSamePassword()
    {
        // Arrange
        var password = "SamePassword123";

        // Act
        var hash1 = _passwordHasher.HashPassword(password);
        var hash2 = _passwordHasher.HashPassword(password);

        // Assert - BCrypt generates different salts
        hash1.Should().NotBe(hash2);
    }

    [Fact]
    public void HashPassword_ShouldReturnValidBcryptHash()
    {
        // Arrange
        var password = "TestPassword123";

        // Act
        var hash = _passwordHasher.HashPassword(password);

        // Assert - BCrypt hash starts with $2a$, $2b$, or $2y$
        hash.Should().MatchRegex(@"^\$2[aby]\$");
    }

    [Fact]
    public void HashPassword_ShouldHandleEmptyPassword()
    {
        // Arrange
        var password = "";

        // Act
        var hash = _passwordHasher.HashPassword(password);

        // Assert
        hash.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void HashPassword_ShouldHandleLongPassword()
    {
        // Arrange
        var password = new string('a', 100);

        // Act
        var hash = _passwordHasher.HashPassword(password);

        // Assert
        hash.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void HashPassword_ShouldHandleSpecialCharacters()
    {
        // Arrange
        var password = "P@ssw0rd!#$%^&*()_+-=[]{}|;':\",./<>?";

        // Act
        var hash = _passwordHasher.HashPassword(password);

        // Assert
        hash.Should().NotBeNullOrEmpty();
    }

    #endregion

    #region VerifyPassword Tests

    [Fact]
    public void VerifyPassword_WithCorrectPassword_ShouldReturnTrue()
    {
        // Arrange
        var password = "CorrectPassword123";
        var hash = _passwordHasher.HashPassword(password);

        // Act
        var result = _passwordHasher.VerifyPassword(password, hash);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void VerifyPassword_WithIncorrectPassword_ShouldReturnFalse()
    {
        // Arrange
        var correctPassword = "CorrectPassword123";
        var wrongPassword = "WrongPassword456";
        var hash = _passwordHasher.HashPassword(correctPassword);

        // Act
        var result = _passwordHasher.VerifyPassword(wrongPassword, hash);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void VerifyPassword_WithEmptyPassword_ShouldReturnFalse()
    {
        // Arrange
        var password = "SomePassword";
        var hash = _passwordHasher.HashPassword(password);

        // Act
        var result = _passwordHasher.VerifyPassword("", hash);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void VerifyPassword_WithInvalidHash_ShouldHandleException()
    {
        // Arrange
        var password = "SomePassword";
        var invalidHash = "not_a_valid_bcrypt_hash";

        // Act & Assert - BCrypt throws on invalid hash
        var act = () => _passwordHasher.VerifyPassword(password, invalidHash);

        // Assert - should throw or return false depending on implementation
        act.Should().Throw<Exception>();
    }

    [Fact]
    public void VerifyPassword_IsCaseSensitive()
    {
        // Arrange
        var password = "Password123";
        var hash = _passwordHasher.HashPassword(password);

        // Act
        var lowerResult = _passwordHasher.VerifyPassword("password123", hash);
        var upperResult = _passwordHasher.VerifyPassword("PASSWORD123", hash);

        // Assert
        lowerResult.Should().BeFalse();
        upperResult.Should().BeFalse();
    }

    [Fact]
    public void VerifyPassword_ShouldWorkWithPreviouslyGeneratedHash()
    {
        // Arrange - generate a hash first
        var password = "testpassword";
        var hash = _passwordHasher.HashPassword(password);

        // Act
        var result = _passwordHasher.VerifyPassword(password, hash);

        // Assert
        result.Should().BeTrue();
    }

    #endregion

    #region Round-trip Tests

    [Theory]
    [InlineData("SimplePassword")]
    [InlineData("With Numbers 123")]
    [InlineData("With-Special_Chars!@#")]
    [InlineData("  SpacesAround  ")]
    [InlineData("Unicode密码")]
    [InlineData("Emoji🔐Password")]
    public void HashPassword_And_VerifyPassword_RoundTrip(string password)
    {
        // Act
        var hash = _passwordHasher.HashPassword(password);
        var result = _passwordHasher.VerifyPassword(password, hash);

        // Assert
        result.Should().BeTrue();
    }

    #endregion
}
