using Loremaster.Application.Common.Interfaces;
using Loremaster.Application.Features.Auth.Commands.Register;
using Loremaster.Domain.Entities;
using Loremaster.Domain.Enums;
using Loremaster.Domain.Exceptions;
using Microsoft.Extensions.Logging;
using Moq;

namespace Loremaster.Tests.Unit.Application.Features.Auth;

/// <summary>
/// Unit tests for RegisterCommandHandler.
/// Tests user registration, role assignment, and master invitation code validation.
/// </summary>
public class RegisterCommandHandlerTests
{
    private readonly Mock<IUserRepository> _userRepositoryMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<IPasswordHasher> _passwordHasherMock;
    private readonly Mock<IJwtTokenGenerator> _jwtTokenGeneratorMock;
    private readonly RegisterCommandHandler _handler;

    private readonly Guid _masterId = Guid.NewGuid();

    public RegisterCommandHandlerTests()
    {
        _userRepositoryMock = new Mock<IUserRepository>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _passwordHasherMock = new Mock<IPasswordHasher>();
        _jwtTokenGeneratorMock = new Mock<IJwtTokenGenerator>();
        _handler = new RegisterCommandHandler(
            _userRepositoryMock.Object,
            _unitOfWorkMock.Object,
            _passwordHasherMock.Object,
            _jwtTokenGeneratorMock.Object);
    }

    #region Success Cases

    [Fact]
    public async Task Handle_WithValidMasterData_ShouldRegisterMaster()
    {
        // Arrange
        var command = new RegisterCommand(
            Email: "master@example.com",
            Password: "Password123",
            DisplayName: "Master User",
            Role: "Master",
            InviteCode: null);

        _userRepositoryMock
            .Setup(x => x.ExistsByEmailAsync(command.Email, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        _passwordHasherMock
            .Setup(x => x.HashPassword(command.Password))
            .Returns("hashed_password");

        _jwtTokenGeneratorMock
            .Setup(x => x.GenerateRefreshToken())
            .Returns("refresh_token");

        _jwtTokenGeneratorMock
            .Setup(x => x.GetRefreshTokenExpiryTime())
            .Returns(DateTime.UtcNow.AddDays(7));

        _jwtTokenGeneratorMock
            .Setup(x => x.GenerateAccessToken(It.IsAny<User>()))
            .Returns("access_token");

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Email.Should().Be(command.Email);
        result.DisplayName.Should().Be(command.DisplayName);
        result.Role.Should().Be("Master");
        result.AccessToken.Should().Be("access_token");
        result.RefreshToken.Should().Be("refresh_token");
        result.InvitationCode.Should().NotBeNullOrEmpty();

        _userRepositoryMock.Verify(
            x => x.AddAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()),
            Times.Once);
        _unitOfWorkMock.Verify(
            x => x.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WithValidPlayerDataAndInviteCode_ShouldRegisterPlayer()
    {
        // Arrange
        var master = User.Create("master@example.com", "hash", "Master", UserRole.Master);
        var invitationCode = master.InvitationCode!;

        var command = new RegisterCommand(
            Email: "player@example.com",
            Password: "Password123",
            DisplayName: "Player User",
            Role: "Player",
            InviteCode: invitationCode);

        _userRepositoryMock
            .Setup(x => x.ExistsByEmailAsync(command.Email, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        _userRepositoryMock
            .Setup(x => x.GetByInvitationCodeAsync(invitationCode, It.IsAny<CancellationToken>()))
            .ReturnsAsync(master);

        _passwordHasherMock
            .Setup(x => x.HashPassword(command.Password))
            .Returns("hashed_password");

        _jwtTokenGeneratorMock
            .Setup(x => x.GenerateRefreshToken())
            .Returns("refresh_token");

        _jwtTokenGeneratorMock
            .Setup(x => x.GetRefreshTokenExpiryTime())
            .Returns(DateTime.UtcNow.AddDays(7));

        _jwtTokenGeneratorMock
            .Setup(x => x.GenerateAccessToken(It.IsAny<User>()))
            .Returns("access_token");

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Role.Should().Be("Player");
        result.MasterId.Should().Be(master.Id.ToString());
    }

    [Fact]
    public async Task Handle_WithAdminRole_ShouldRegisterAdmin()
    {
        // Arrange
        var command = new RegisterCommand(
            Email: "admin@example.com",
            Password: "Password123",
            DisplayName: "Admin User",
            Role: "Admin",
            InviteCode: null);

        _userRepositoryMock
            .Setup(x => x.ExistsByEmailAsync(command.Email, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        _passwordHasherMock
            .Setup(x => x.HashPassword(command.Password))
            .Returns("hashed_password");

        _jwtTokenGeneratorMock
            .Setup(x => x.GenerateRefreshToken())
            .Returns("refresh_token");

        _jwtTokenGeneratorMock
            .Setup(x => x.GetRefreshTokenExpiryTime())
            .Returns(DateTime.UtcNow.AddDays(7));

        _jwtTokenGeneratorMock
            .Setup(x => x.GenerateAccessToken(It.IsAny<User>()))
            .Returns("access_token");

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Role.Should().Be("Admin");
    }

    [Fact]
    public async Task Handle_WithCaseInsensitiveRole_ShouldParseCorrectly()
    {
        // Arrange
        var command = new RegisterCommand(
            Email: "master2@example.com",
            Password: "Password123",
            DisplayName: "Master User",
            Role: "MASTER",
            InviteCode: null);

        _userRepositoryMock
            .Setup(x => x.ExistsByEmailAsync(command.Email, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        _passwordHasherMock
            .Setup(x => x.HashPassword(command.Password))
            .Returns("hashed_password");

        _jwtTokenGeneratorMock
            .Setup(x => x.GenerateRefreshToken())
            .Returns("refresh_token");

        _jwtTokenGeneratorMock
            .Setup(x => x.GetRefreshTokenExpiryTime())
            .Returns(DateTime.UtcNow.AddDays(7));

        _jwtTokenGeneratorMock
            .Setup(x => x.GenerateAccessToken(It.IsAny<User>()))
            .Returns("access_token");

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Role.Should().Be("Master");
    }

    #endregion

    #region Validation Failures

    [Fact]
    public async Task Handle_WhenEmailExists_ShouldThrowDomainException()
    {
        // Arrange
        var command = new RegisterCommand(
            Email: "existing@example.com",
            Password: "Password123",
            DisplayName: "User",
            Role: "Master",
            InviteCode: null);

        _userRepositoryMock
            .Setup(x => x.ExistsByEmailAsync(command.Email, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var act = () => _handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<DomainException>()
            .WithMessage("*email already exists*");
    }

    [Fact]
    public async Task Handle_WithInvalidRole_ShouldThrowDomainException()
    {
        // Arrange
        var command = new RegisterCommand(
            Email: "user@example.com",
            Password: "Password123",
            DisplayName: "User",
            Role: "InvalidRole",
            InviteCode: null);

        _userRepositoryMock
            .Setup(x => x.ExistsByEmailAsync(command.Email, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act
        var act = () => _handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<DomainException>()
            .WithMessage("*Invalid role*");
    }

    [Fact]
    public async Task Handle_WithInvalidInviteCode_ShouldThrowDomainException()
    {
        // Arrange
        var command = new RegisterCommand(
            Email: "player@example.com",
            Password: "Password123",
            DisplayName: "Player",
            Role: "Player",
            InviteCode: "INVALID");

        _userRepositoryMock
            .Setup(x => x.ExistsByEmailAsync(command.Email, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        _userRepositoryMock
            .Setup(x => x.GetByInvitationCodeAsync(command.InviteCode, It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        // Act
        var act = () => _handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<DomainException>()
            .WithMessage("*Invalid invitation code*");
    }

    [Fact]
    public async Task Handle_WhenPlayerWithoutInviteCode_ShouldThrowDomainException()
    {
        // Arrange
        var command = new RegisterCommand(
            Email: "player@example.com",
            Password: "Password123",
            DisplayName: "Player",
            Role: "Player",
            InviteCode: null);

        _userRepositoryMock
            .Setup(x => x.ExistsByEmailAsync(command.Email, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act
        var act = () => _handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<DomainException>()
            .WithMessage("*Players must be associated with a Master*");
    }

    #endregion
}
