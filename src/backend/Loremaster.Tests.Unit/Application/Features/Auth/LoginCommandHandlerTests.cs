using Loremaster.Application.Common.Interfaces;
using Loremaster.Application.Features.Auth.Commands.Login;
using Loremaster.Domain.Entities;
using Loremaster.Domain.Enums;
using Loremaster.Domain.Exceptions;
using Moq;

namespace Loremaster.Tests.Unit.Application.Features.Auth;

/// <summary>
/// Unit tests for LoginCommandHandler.
/// Tests authentication, credential validation, and token generation.
/// </summary>
public class LoginCommandHandlerTests
{
    private readonly Mock<IUserRepository> _userRepositoryMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<IPasswordHasher> _passwordHasherMock;
    private readonly Mock<IJwtTokenGenerator> _jwtTokenGeneratorMock;
    private readonly LoginCommandHandler _handler;

    public LoginCommandHandlerTests()
    {
        _userRepositoryMock = new Mock<IUserRepository>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _passwordHasherMock = new Mock<IPasswordHasher>();
        _jwtTokenGeneratorMock = new Mock<IJwtTokenGenerator>();
        _handler = new LoginCommandHandler(
            _userRepositoryMock.Object,
            _unitOfWorkMock.Object,
            _passwordHasherMock.Object,
            _jwtTokenGeneratorMock.Object);
    }

    #region Success Cases

    [Fact]
    public async Task Handle_WithValidCredentials_ShouldReturnTokens()
    {
        // Arrange
        var user = User.Create("user@example.com", "hashed_password", "Test User", UserRole.Player);
        
        var command = new LoginCommand(
            Email: "user@example.com",
            Password: "Password123");

        _userRepositoryMock
            .Setup(x => x.GetByEmailAsync(command.Email, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        _passwordHasherMock
            .Setup(x => x.VerifyPassword(command.Password, user.PasswordHash))
            .Returns(true);

        _jwtTokenGeneratorMock
            .Setup(x => x.GenerateRefreshToken())
            .Returns("refresh_token");

        _jwtTokenGeneratorMock
            .Setup(x => x.GetRefreshTokenExpiryTime())
            .Returns(DateTime.UtcNow.AddDays(7));

        _jwtTokenGeneratorMock
            .Setup(x => x.GenerateAccessToken(user))
            .Returns("access_token");

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Email.Should().Be(user.Email);
        result.DisplayName.Should().Be(user.DisplayName);
        result.Role.Should().Be("Player");
        result.AccessToken.Should().Be("access_token");
        result.RefreshToken.Should().Be("refresh_token");

        _userRepositoryMock.Verify(
            x => x.UpdateAsync(user, It.IsAny<CancellationToken>()),
            Times.Once);
        _unitOfWorkMock.Verify(
            x => x.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WithMasterUser_ShouldReturnInvitationCode()
    {
        // Arrange
        var master = User.Create("master@example.com", "hashed_password", "Master", UserRole.Master);
        
        var command = new LoginCommand(
            Email: "master@example.com",
            Password: "Password123");

        _userRepositoryMock
            .Setup(x => x.GetByEmailAsync(command.Email, It.IsAny<CancellationToken>()))
            .ReturnsAsync(master);

        _passwordHasherMock
            .Setup(x => x.VerifyPassword(command.Password, master.PasswordHash))
            .Returns(true);

        _jwtTokenGeneratorMock
            .Setup(x => x.GenerateRefreshToken())
            .Returns("refresh_token");

        _jwtTokenGeneratorMock
            .Setup(x => x.GetRefreshTokenExpiryTime())
            .Returns(DateTime.UtcNow.AddDays(7));

        _jwtTokenGeneratorMock
            .Setup(x => x.GenerateAccessToken(master))
            .Returns("access_token");

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.InvitationCode.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task Handle_ShouldUpdateLastLoginAt()
    {
        // Arrange
        var user = User.Create("user@example.com", "hashed_password", "Test User", UserRole.Player);
        var beforeLogin = DateTime.UtcNow;

        var command = new LoginCommand(
            Email: "user@example.com",
            Password: "Password123");

        _userRepositoryMock
            .Setup(x => x.GetByEmailAsync(command.Email, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        _passwordHasherMock
            .Setup(x => x.VerifyPassword(command.Password, user.PasswordHash))
            .Returns(true);

        _jwtTokenGeneratorMock
            .Setup(x => x.GenerateRefreshToken())
            .Returns("refresh_token");

        _jwtTokenGeneratorMock
            .Setup(x => x.GetRefreshTokenExpiryTime())
            .Returns(DateTime.UtcNow.AddDays(7));

        _jwtTokenGeneratorMock
            .Setup(x => x.GenerateAccessToken(user))
            .Returns("access_token");

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        user.LastLoginAt.Should().NotBeNull();
        user.LastLoginAt.Should().BeOnOrAfter(beforeLogin);
    }

    #endregion

    #region Failure Cases

    [Fact]
    public async Task Handle_WithNonExistentUser_ShouldThrowDomainException()
    {
        // Arrange
        var command = new LoginCommand(
            Email: "nonexistent@example.com",
            Password: "Password123");

        _userRepositoryMock
            .Setup(x => x.GetByEmailAsync(command.Email, It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        // Act
        var act = () => _handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<DomainException>()
            .WithMessage("*Invalid credentials*");
    }

    [Fact]
    public async Task Handle_WithWrongPassword_ShouldThrowDomainException()
    {
        // Arrange
        var user = User.Create("user@example.com", "hashed_password", "Test User", UserRole.Player);

        var command = new LoginCommand(
            Email: "user@example.com",
            Password: "WrongPassword");

        _userRepositoryMock
            .Setup(x => x.GetByEmailAsync(command.Email, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        _passwordHasherMock
            .Setup(x => x.VerifyPassword(command.Password, user.PasswordHash))
            .Returns(false);

        // Act
        var act = () => _handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<DomainException>()
            .WithMessage("*Invalid credentials*");
    }

    [Fact]
    public async Task Handle_WithDeactivatedUser_ShouldThrowForbiddenAccessException()
    {
        // Arrange
        var user = User.Create("user@example.com", "hashed_password", "Test User", UserRole.Player);
        user.Deactivate();

        var command = new LoginCommand(
            Email: "user@example.com",
            Password: "Password123");

        _userRepositoryMock
            .Setup(x => x.GetByEmailAsync(command.Email, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        _passwordHasherMock
            .Setup(x => x.VerifyPassword(command.Password, user.PasswordHash))
            .Returns(true);

        // Act
        var act = () => _handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ForbiddenAccessException>()
            .WithMessage("*deactivated*");
    }

    #endregion
}
