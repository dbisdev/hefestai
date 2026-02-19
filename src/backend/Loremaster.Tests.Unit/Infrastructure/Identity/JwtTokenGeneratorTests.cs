using Loremaster.Application.Common.Interfaces;
using Loremaster.Domain.Entities;
using Loremaster.Domain.Enums;
using Loremaster.Infrastructure.Identity;
using Microsoft.Extensions.Configuration;
using Moq;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace Loremaster.Tests.Unit.Infrastructure.Identity;

/// <summary>
/// Unit tests for JwtTokenGenerator.
/// Tests JWT token generation, refresh token creation, and expiry times.
/// </summary>
public class JwtTokenGeneratorTests
{
    private readonly Mock<IDateTimeProvider> _dateTimeProviderMock;
    private readonly JwtTokenGenerator _tokenGenerator;
    private readonly DateTime _fixedNow = new DateTime(2024, 1, 15, 12, 0, 0, DateTimeKind.Utc);

    public JwtTokenGeneratorTests()
    {
        _dateTimeProviderMock = new Mock<IDateTimeProvider>();
        _dateTimeProviderMock
            .Setup(x => x.UtcNow)
            .Returns(_fixedNow);

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Secret"] = "ThisIsATestSecretKeyThatIsAtLeast32CharactersLong!",
                ["Jwt:Issuer"] = "Loremaster",
                ["Jwt:Audience"] = "LoremasterUsers",
                ["Jwt:AccessTokenExpirationMinutes"] = "15",
                ["Jwt:RefreshTokenExpirationDays"] = "7"
            })
            .Build();

        _tokenGenerator = new JwtTokenGenerator(configuration, _dateTimeProviderMock.Object);
    }

    #region GenerateAccessToken Tests

    [Fact]
    public void GenerateAccessToken_WithValidUser_ShouldReturnValidToken()
    {
        // Arrange
        var user = User.Create("user@example.com", "hash", "Test User", UserRole.Player);

        // Act
        var token = _tokenGenerator.GenerateAccessToken(user);

        // Assert
        token.Should().NotBeNullOrEmpty();
        
        var handler = new JwtSecurityTokenHandler();
        var jwtToken = handler.ReadJwtToken(token);
        
        jwtToken.Should().NotBeNull();
        jwtToken.Issuer.Should().Be("Loremaster");
        jwtToken.Audiences.Should().Contain("LoremasterUsers");
    }

    [Fact]
    public void GenerateAccessToken_ShouldIncludeCorrectClaims()
    {
        // Arrange
        var user = User.Create("user@example.com", "hash", "Test User", UserRole.Master);

        // Act
        var token = _tokenGenerator.GenerateAccessToken(user);
        var handler = new JwtSecurityTokenHandler();
        var jwtToken = handler.ReadJwtToken(token);

        // Assert
        var claims = jwtToken.Claims.ToDictionary(c => c.Type, c => c.Value);
        
        claims[JwtRegisteredClaimNames.Sub].Should().Be(user.Id.ToString());
        claims[JwtRegisteredClaimNames.Email].Should().Be(user.Email);
        claims[JwtTokenGenerator.RoleClaimType].Should().Be("Master");
        claims["displayName"].Should().Be("Test User");
        claims.Should().ContainKey(JwtRegisteredClaimNames.Jti);
    }

    [Fact]
    public void GenerateAccessToken_ShouldSetCorrectExpiration()
    {
        // Arrange
        var user = User.Create("user@example.com", "hash", "Test User", UserRole.Player);

        // Act
        var token = _tokenGenerator.GenerateAccessToken(user);
        var handler = new JwtSecurityTokenHandler();
        var jwtToken = handler.ReadJwtToken(token);

        // Assert
        var expectedExpiry = _fixedNow.AddMinutes(15);
        jwtToken.ValidTo.Should().BeCloseTo(expectedExpiry, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void GenerateAccessToken_WithAdminRole_ShouldIncludeAdminRole()
    {
        // Arrange
        var user = User.Create("admin@example.com", "hash", "Admin", UserRole.Admin);

        // Act
        var token = _tokenGenerator.GenerateAccessToken(user);
        var handler = new JwtSecurityTokenHandler();
        var jwtToken = handler.ReadJwtToken(token);

        // Assert
        var roleClaim = jwtToken.Claims.First(c => c.Type == JwtTokenGenerator.RoleClaimType);
        roleClaim.Value.Should().Be("Admin");
    }

    [Fact]
    public void GenerateAccessToken_WithNullDisplayName_ShouldUseEmptyString()
    {
        // Arrange
        var user = User.Create("user@example.com", "hash", null, UserRole.Player);

        // Act
        var token = _tokenGenerator.GenerateAccessToken(user);
        var handler = new JwtSecurityTokenHandler();
        var jwtToken = handler.ReadJwtToken(token);

        // Assert
        var displayNameClaim = jwtToken.Claims.First(c => c.Type == "displayName");
        displayNameClaim.Value.Should().BeEmpty();
    }

    #endregion

    #region GenerateRefreshToken Tests

    [Fact]
    public void GenerateRefreshToken_ShouldReturnNonEmptyToken()
    {
        // Act
        var refreshToken = _tokenGenerator.GenerateRefreshToken();

        // Assert
        refreshToken.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void GenerateRefreshToken_ShouldReturnUniqueTokens()
    {
        // Act
        var token1 = _tokenGenerator.GenerateRefreshToken();
        var token2 = _tokenGenerator.GenerateRefreshToken();
        var token3 = _tokenGenerator.GenerateRefreshToken();

        // Assert
        token1.Should().NotBe(token2);
        token2.Should().NotBe(token3);
        token1.Should().NotBe(token3);
    }

    [Fact]
    public void GenerateRefreshToken_ShouldReturnBase64String()
    {
        // Act
        var refreshToken = _tokenGenerator.GenerateRefreshToken();

        // Assert
        refreshToken.Should().MatchRegex("^[A-Za-z0-9+/]+=*$");
    }

    [Fact]
    public void GenerateRefreshToken_ShouldReturnTokenOfCorrectLength()
    {
        // Act
        var refreshToken = _tokenGenerator.GenerateRefreshToken();

        // 64 bytes encoded as Base64 = 88 characters
        refreshToken.Length.Should().Be(88);
    }

    #endregion

    #region GetRefreshTokenExpiryTime Tests

    [Fact]
    public void GetRefreshTokenExpiryTime_ShouldReturnCorrectTime()
    {
        // Act
        var expiryTime = _tokenGenerator.GetRefreshTokenExpiryTime();

        // Assert
        var expectedExpiry = _fixedNow.AddDays(7);
        expiryTime.Should().Be(expectedExpiry);
    }

    [Fact]
    public void GetRefreshTokenExpiryTime_ShouldReturnUtcTime()
    {
        // Act
        var expiryTime = _tokenGenerator.GetRefreshTokenExpiryTime();

        // Assert
        expiryTime.Kind.Should().Be(DateTimeKind.Utc);
    }

    #endregion
}
