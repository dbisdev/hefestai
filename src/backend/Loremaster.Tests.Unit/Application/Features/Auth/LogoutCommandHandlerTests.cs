using Loremaster.Application.Common.Interfaces;
using Loremaster.Application.Features.Auth.Commands.Logout;
using Loremaster.Domain.Entities;
using Loremaster.Domain.Enums;
using MediatR;
using Moq;

namespace Loremaster.Tests.Unit.Application.Features.Auth;

/// <summary>
/// Unit tests for LogoutCommandHandler.
/// Tests token revocation and session management.
/// </summary>
public class LogoutCommandHandlerTests
{
    private readonly Mock<ICurrentUserService> _currentUserServiceMock;
    private readonly Mock<IUserRepository> _userRepositoryMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly LogoutCommandHandler _handler;

    public LogoutCommandHandlerTests()
    {
        _currentUserServiceMock = new Mock<ICurrentUserService>();
        _userRepositoryMock = new Mock<IUserRepository>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _handler = new LogoutCommandHandler(
            _currentUserServiceMock.Object,
            _userRepositoryMock.Object,
            _unitOfWorkMock.Object);
    }

    [Fact]
    public async Task Handle_WhenAuthenticated_ShouldRevokeRefreshToken()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = User.Create("user@example.com", "hash", "User", UserRole.Player);
        user.SetRefreshToken("refresh_token", DateTime.UtcNow.AddDays(7));

        _currentUserServiceMock
            .Setup(x => x.UserId)
            .Returns(userId);

        _userRepositoryMock
            .Setup(x => x.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        // Act
        var result = await _handler.Handle(new LogoutCommand(), CancellationToken.None);

        // Assert
        result.Should().Be(MediatR.Unit.Value);
        user.RefreshToken.Should().BeNull();
        user.RefreshTokenExpiryTime.Should().BeNull();

        _userRepositoryMock.Verify(
            x => x.UpdateAsync(user, It.IsAny<CancellationToken>()),
            Times.Once);
        _unitOfWorkMock.Verify(
            x => x.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WhenNotAuthenticated_ShouldDoNothing()
    {
        // Arrange
        _currentUserServiceMock
            .Setup(x => x.UserId)
            .Returns((Guid?)null);

        // Act
        var result = await _handler.Handle(new LogoutCommand(), CancellationToken.None);

        // Assert
        result.Should().Be(MediatR.Unit.Value);

        _userRepositoryMock.Verify(
            x => x.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
            Times.Never);
        _unitOfWorkMock.Verify(
            x => x.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_WhenUserNotFound_ShouldDoNothing()
    {
        // Arrange
        var userId = Guid.NewGuid();

        _currentUserServiceMock
            .Setup(x => x.UserId)
            .Returns(userId);

        _userRepositoryMock
            .Setup(x => x.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        // Act
        var result = await _handler.Handle(new LogoutCommand(), CancellationToken.None);

        // Assert
        result.Should().Be(MediatR.Unit.Value);

        _userRepositoryMock.Verify(
            x => x.UpdateAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()),
            Times.Never);
        _unitOfWorkMock.Verify(
            x => x.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Never);
    }
}
