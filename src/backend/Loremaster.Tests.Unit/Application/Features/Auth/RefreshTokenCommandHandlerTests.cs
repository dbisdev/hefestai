using Loremaster.Application.Common.Interfaces;
using Loremaster.Application.Features.Auth.Commands.RefreshToken;
using Loremaster.Domain.Entities;
using Loremaster.Domain.Enums;
using Loremaster.Domain.Exceptions;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace Loremaster.Tests.Unit.Application.Features.Auth;

/// <summary>
/// Unit tests for RefreshTokenCommandHandler.
/// Tests token refresh logic and validation.
/// </summary>
public class RefreshTokenCommandHandlerTests
{
    private readonly Mock<IApplicationDbContext> _contextMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<IJwtTokenGenerator> _jwtTokenGeneratorMock;
    private readonly Mock<IDateTimeProvider> _dateTimeProviderMock;
    private readonly Mock<DbSet<User>> _usersDbSetMock;
    private readonly RefreshTokenCommandHandler _handler;

    public RefreshTokenCommandHandlerTests()
    {
        _contextMock = new Mock<IApplicationDbContext>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _jwtTokenGeneratorMock = new Mock<IJwtTokenGenerator>();
        _dateTimeProviderMock = new Mock<IDateTimeProvider>();
        _usersDbSetMock = new Mock<DbSet<User>>();

        _contextMock
            .Setup(x => x.Users)
            .Returns(_usersDbSetMock.Object);

        _handler = new RefreshTokenCommandHandler(
            _contextMock.Object,
            _unitOfWorkMock.Object,
            _jwtTokenGeneratorMock.Object,
            _dateTimeProviderMock.Object);
    }

    [Fact]
    public async Task Handle_WithValidRefreshToken_ShouldReturnNewTokens()
    {
        // Arrange
        var user = User.Create("user@example.com", "hash", "User", UserRole.Player);
        var refreshToken = "valid_refresh_token";
        user.SetRefreshToken(refreshToken, DateTime.UtcNow.AddDays(7));

        var users = new List<User> { user }.AsQueryable();

        _usersDbSetMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<System.Linq.Expressions.Expression<Func<User, bool>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        _dateTimeProviderMock
            .Setup(x => x.UtcNow)
            .Returns(DateTime.UtcNow);

        _jwtTokenGeneratorMock
            .Setup(x => x.GenerateRefreshToken())
            .Returns("new_refresh_token");

        _jwtTokenGeneratorMock
            .Setup(x => x.GetRefreshTokenExpiryTime())
            .Returns(DateTime.UtcNow.AddDays(7));

        _jwtTokenGeneratorMock
            .Setup(x => x.GenerateAccessToken(user))
            .Returns("new_access_token");

        var command = new RefreshTokenCommand(refreshToken);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.AccessToken.Should().Be("new_access_token");
        result.RefreshToken.Should().Be("new_refresh_token");

        _unitOfWorkMock.Verify(
            x => x.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WithInvalidRefreshToken_ShouldThrowDomainException()
    {
        // Arrange
        _usersDbSetMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<System.Linq.Expressions.Expression<Func<User, bool>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        var command = new RefreshTokenCommand("invalid_token");

        // Act
        var act = () => _handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<DomainException>()
            .WithMessage("*Invalid refresh token*");
    }

    [Fact]
    public async Task Handle_WithExpiredRefreshToken_ShouldThrowDomainException()
    {
        // Arrange
        var user = User.Create("user@example.com", "hash", "User", UserRole.Player);
        var expiredToken = "expired_token";
        user.SetRefreshToken(expiredToken, DateTime.UtcNow.AddDays(-1)); // Expired

        _usersDbSetMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<System.Linq.Expressions.Expression<Func<User, bool>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        _dateTimeProviderMock
            .Setup(x => x.UtcNow)
            .Returns(DateTime.UtcNow);

        var command = new RefreshTokenCommand(expiredToken);

        // Act
        var act = () => _handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<DomainException>()
            .WithMessage("*expired*");
    }

    [Fact]
    public async Task Handle_WithDeactivatedUser_ShouldThrowForbiddenAccessException()
    {
        // Arrange
        var user = User.Create("user@example.com", "hash", "User", UserRole.Player);
        var refreshToken = "valid_refresh_token";
        user.SetRefreshToken(refreshToken, DateTime.UtcNow.AddDays(7));
        user.Deactivate();

        _usersDbSetMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<System.Linq.Expressions.Expression<Func<User, bool>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        _dateTimeProviderMock
            .Setup(x => x.UtcNow)
            .Returns(DateTime.UtcNow);

        var command = new RefreshTokenCommand(refreshToken);

        // Act
        var act = () => _handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ForbiddenAccessException>()
            .WithMessage("*deactivated*");
    }
}
