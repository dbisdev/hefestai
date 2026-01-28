using System.IdentityModel.Tokens.Jwt;
using Loremaster.Application.Common.Interfaces;
using Loremaster.Infrastructure.Identity;
using Microsoft.Extensions.Configuration;
using Moq;

namespace Loremaster.Tests.Unit.Infrastructure.Identity;

public class ServiceTokenGeneratorTests
{
    private readonly Mock<IConfiguration> _configurationMock;
    private readonly Mock<IDateTimeProvider> _dateTimeProviderMock;
    private readonly ServiceTokenGenerator _sut;

    public ServiceTokenGeneratorTests()
    {
        _configurationMock = new Mock<IConfiguration>();
        _dateTimeProviderMock = new Mock<IDateTimeProvider>();

        // Setup default configuration
        _configurationMock.Setup(x => x["ServiceJwt:Secret"])
            .Returns("test-secret-key-that-is-at-least-32-characters-long");
        _configurationMock.Setup(x => x["ServiceJwt:Issuer"])
            .Returns("Loremaster.Api");
        _configurationMock.Setup(x => x["ServiceJwt:Audience"])
            .Returns("Loremaster.Genkit");
        _configurationMock.Setup(x => x["ServiceJwt:ExpirationMinutes"])
            .Returns("5");

        _dateTimeProviderMock.Setup(x => x.UtcNow)
            .Returns(new DateTime(2024, 1, 15, 12, 0, 0, DateTimeKind.Utc));

        _sut = new ServiceTokenGenerator(_configurationMock.Object, _dateTimeProviderMock.Object);
    }

    [Fact]
    public void GenerateServiceToken_ReturnsValidJwtToken()
    {
        // Act
        var token = _sut.GenerateServiceToken(ServiceScopes.GenkitExecute);

        // Assert
        token.Should().NotBeNullOrEmpty();
        
        var handler = new JwtSecurityTokenHandler();
        handler.CanReadToken(token).Should().BeTrue();
    }

    [Fact]
    public void GenerateServiceToken_TokenContainsCorrectIssuer()
    {
        // Act
        var token = _sut.GenerateServiceToken(ServiceScopes.GenkitExecute);

        // Assert
        var handler = new JwtSecurityTokenHandler();
        var jwtToken = handler.ReadJwtToken(token);
        
        jwtToken.Issuer.Should().Be("Loremaster.Api");
    }

    [Fact]
    public void GenerateServiceToken_TokenContainsCorrectAudience()
    {
        // Act
        var token = _sut.GenerateServiceToken(ServiceScopes.GenkitExecute);

        // Assert
        var handler = new JwtSecurityTokenHandler();
        var jwtToken = handler.ReadJwtToken(token);
        
        jwtToken.Audiences.Should().Contain("Loremaster.Genkit");
    }

    [Fact]
    public void GenerateServiceToken_TokenContainsSubjectClaim()
    {
        // Act
        var token = _sut.GenerateServiceToken(ServiceScopes.GenkitExecute);

        // Assert
        var handler = new JwtSecurityTokenHandler();
        var jwtToken = handler.ReadJwtToken(token);
        
        jwtToken.Subject.Should().Be("loremaster-backend");
    }

    [Fact]
    public void GenerateServiceToken_TokenContainsScopeClaim()
    {
        // Act
        var token = _sut.GenerateServiceToken(ServiceScopes.GenkitExecute);

        // Assert
        var handler = new JwtSecurityTokenHandler();
        var jwtToken = handler.ReadJwtToken(token);
        
        var scopeClaim = jwtToken.Claims.FirstOrDefault(c => c.Type == "scope");
        scopeClaim.Should().NotBeNull();
        scopeClaim!.Value.Should().Be(ServiceScopes.GenkitExecute);
    }

    [Fact]
    public void GenerateServiceToken_WithMultipleScopes_CombinesScopesWithSpace()
    {
        // Act
        var token = _sut.GenerateServiceToken(ServiceScopes.GenkitExecute, ServiceScopes.GenkitAdmin);

        // Assert
        var handler = new JwtSecurityTokenHandler();
        var jwtToken = handler.ReadJwtToken(token);
        
        var scopeClaim = jwtToken.Claims.FirstOrDefault(c => c.Type == "scope");
        scopeClaim.Should().NotBeNull();
        scopeClaim!.Value.Should().Contain(ServiceScopes.GenkitExecute);
        scopeClaim.Value.Should().Contain(ServiceScopes.GenkitAdmin);
        scopeClaim.Value.Should().Contain(" ");
    }

    [Fact]
    public void GenerateServiceToken_TokenContainsJtiClaim()
    {
        // Act
        var token = _sut.GenerateServiceToken(ServiceScopes.GenkitExecute);

        // Assert
        var handler = new JwtSecurityTokenHandler();
        var jwtToken = handler.ReadJwtToken(token);
        
        var jtiClaim = jwtToken.Claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Jti);
        jtiClaim.Should().NotBeNull();
        Guid.TryParse(jtiClaim!.Value, out _).Should().BeTrue();
    }

    [Fact]
    public void GenerateServiceToken_TokenHasCorrectExpiration()
    {
        // Arrange
        var expectedExpiration = new DateTime(2024, 1, 15, 12, 5, 0, DateTimeKind.Utc);

        // Act
        var token = _sut.GenerateServiceToken(ServiceScopes.GenkitExecute);

        // Assert
        var handler = new JwtSecurityTokenHandler();
        var jwtToken = handler.ReadJwtToken(token);
        
        jwtToken.ValidTo.Should().BeCloseTo(expectedExpiration, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void GenerateServiceToken_GeneratesUniqueTokensEachCall()
    {
        // Act
        var token1 = _sut.GenerateServiceToken(ServiceScopes.GenkitExecute);
        var token2 = _sut.GenerateServiceToken(ServiceScopes.GenkitExecute);

        // Assert
        token1.Should().NotBe(token2);
    }

    [Fact]
    public void GenerateServiceToken_WhenSecretNotConfigured_ThrowsInvalidOperationException()
    {
        // Arrange
        _configurationMock.Setup(x => x["ServiceJwt:Secret"]).Returns((string?)null);
        var sut = new ServiceTokenGenerator(_configurationMock.Object, _dateTimeProviderMock.Object);

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() =>
            sut.GenerateServiceToken(ServiceScopes.GenkitExecute));

        exception.Message.Should().Contain("ServiceJwt:Secret");
    }

    [Fact]
    public void GenerateServiceToken_UsesDefaultIssuerWhenNotConfigured()
    {
        // Arrange
        _configurationMock.Setup(x => x["ServiceJwt:Issuer"]).Returns((string?)null);
        var sut = new ServiceTokenGenerator(_configurationMock.Object, _dateTimeProviderMock.Object);

        // Act
        var token = sut.GenerateServiceToken(ServiceScopes.GenkitExecute);

        // Assert
        var handler = new JwtSecurityTokenHandler();
        var jwtToken = handler.ReadJwtToken(token);
        jwtToken.Issuer.Should().Be("Loremaster.Api");
    }

    [Fact]
    public void GenerateServiceToken_UsesDefaultAudienceWhenNotConfigured()
    {
        // Arrange
        _configurationMock.Setup(x => x["ServiceJwt:Audience"]).Returns((string?)null);
        var sut = new ServiceTokenGenerator(_configurationMock.Object, _dateTimeProviderMock.Object);

        // Act
        var token = sut.GenerateServiceToken(ServiceScopes.GenkitExecute);

        // Assert
        var handler = new JwtSecurityTokenHandler();
        var jwtToken = handler.ReadJwtToken(token);
        jwtToken.Audiences.Should().Contain("Loremaster.Genkit");
    }

    [Fact]
    public void GenerateServiceToken_UsesDefaultExpirationWhenNotConfigured()
    {
        // Arrange
        _configurationMock.Setup(x => x["ServiceJwt:ExpirationMinutes"]).Returns((string?)null);
        var sut = new ServiceTokenGenerator(_configurationMock.Object, _dateTimeProviderMock.Object);
        var expectedExpiration = new DateTime(2024, 1, 15, 12, 5, 0, DateTimeKind.Utc);

        // Act
        var token = sut.GenerateServiceToken(ServiceScopes.GenkitExecute);

        // Assert
        var handler = new JwtSecurityTokenHandler();
        var jwtToken = handler.ReadJwtToken(token);
        jwtToken.ValidTo.Should().BeCloseTo(expectedExpiration, TimeSpan.FromSeconds(1));
    }
}
