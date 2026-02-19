using Loremaster.Application.Common.Interfaces;
using Loremaster.Application.Features.Auth.Queries.GetCurrentUser;
using Loremaster.Domain.Entities;
using Loremaster.Domain.Enums;
using Loremaster.Domain.Exceptions;
using Moq;

namespace Loremaster.Tests.Unit.Application.Features.Auth;

/// <summary>
/// Unit tests for GetCurrentUserQueryHandler.
/// Tests retrieval of current authenticated user.
/// </summary>
public class GetCurrentUserQueryHandlerTests
{
    private readonly Mock<ICurrentUserService> _currentUserServiceMock;
    private readonly Mock<IUserRepository> _userRepositoryMock;
    private readonly GetCurrentUserQueryHandler _handler;

    public GetCurrentUserQueryHandlerTests()
    {
        _currentUserServiceMock = new Mock<ICurrentUserService>();
        _userRepositoryMock = new Mock<IUserRepository>();
        _handler = new GetCurrentUserQueryHandler(
            _userRepositoryMock.Object,
            _currentUserServiceMock.Object);
    }

    [Fact]
    public async Task Handle_WhenAuthenticated_ShouldReturnCurrentUser()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = User.Create("user@example.com", "hash", "Test User", UserRole.Player);
        
        _currentUserServiceMock
            .Setup(x => x.UserId)
            .Returns(userId);

        _userRepositoryMock
            .Setup(x => x.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        // Act
        var result = await _handler.Handle(new GetCurrentUserQuery(), CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Email.Should().Be(user.Email);
        result.DisplayName.Should().Be(user.DisplayName);
        result.Role.Should().Be("Player");
    }

    [Fact]
    public async Task Handle_WhenNotAuthenticated_ShouldThrowForbiddenAccessException()
    {
        // Arrange
        _currentUserServiceMock
            .Setup(x => x.UserId)
            .Returns((Guid?)null);

        // Act
        var act = () => _handler.Handle(new GetCurrentUserQuery(), CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ForbiddenAccessException>()
            .WithMessage("*authenticated*");
    }

    [Fact]
    public async Task Handle_WhenUserNotFound_ShouldThrowNotFoundException()
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
        var act = () => _handler.Handle(new GetCurrentUserQuery(), CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<NotFoundException>()
            .WithMessage("*User*");
    }

    [Fact]
    public async Task Handle_ForMasterUser_ShouldReturnInvitationCode()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var master = User.Create("master@example.com", "hash", "Master", UserRole.Master);

        _currentUserServiceMock
            .Setup(x => x.UserId)
            .Returns(userId);

        _userRepositoryMock
            .Setup(x => x.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(master);

        // Act
        var result = await _handler.Handle(new GetCurrentUserQuery(), CancellationToken.None);

        // Assert
        result.InvitationCode.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task Handle_ForPlayerWithMaster_ShouldReturnMasterId()
    {
        // Arrange
        var masterId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var player = User.Create("player@example.com", "hash", "Player", UserRole.Player, masterId);

        _currentUserServiceMock
            .Setup(x => x.UserId)
            .Returns(userId);

        _userRepositoryMock
            .Setup(x => x.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(player);

        // Act
        var result = await _handler.Handle(new GetCurrentUserQuery(), CancellationToken.None);

        // Assert
        result.MasterId.Should().Be(masterId.ToString());
    }
}
