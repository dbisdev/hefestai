using Loremaster.Application.Common.Interfaces;
using Loremaster.Application.Features.Auth.Commands.Register;
using Loremaster.Domain.Entities;
using Loremaster.Domain.Enums;
using Loremaster.Domain.Exceptions;
using Moq;

namespace Loremaster.Tests.Unit.Application.Features.Auth;

/// <summary>
/// Unit tests for RegisterCommandHandler.
/// Tests user registration with optional campaign joining via invite code.
/// </summary>
public class RegisterCommandHandlerTests
{
    private readonly Mock<IUserRepository> _userRepositoryMock;
    private readonly Mock<ICampaignRepository> _campaignRepositoryMock;
    private readonly Mock<ICampaignMemberRepository> _campaignMemberRepositoryMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<IPasswordHasher> _passwordHasherMock;
    private readonly Mock<IJwtTokenGenerator> _jwtTokenGeneratorMock;
    private readonly RegisterCommandHandler _handler;

    public RegisterCommandHandlerTests()
    {
        _userRepositoryMock = new Mock<IUserRepository>();
        _campaignRepositoryMock = new Mock<ICampaignRepository>();
        _campaignMemberRepositoryMock = new Mock<ICampaignMemberRepository>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _passwordHasherMock = new Mock<IPasswordHasher>();
        _jwtTokenGeneratorMock = new Mock<IJwtTokenGenerator>();
        _handler = new RegisterCommandHandler(
            _userRepositoryMock.Object,
            _campaignRepositoryMock.Object,
            _campaignMemberRepositoryMock.Object,
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

        _userRepositoryMock.Verify(
            x => x.AddAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()),
            Times.Once);
        _unitOfWorkMock.Verify(
            x => x.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WithValidPlayerDataAndInviteCode_ShouldRegisterPlayerAndJoinCampaign()
    {
        // Arrange
        var campaign = Campaign.Create(
            name: "Test Campaign",
            gameSystemId: Guid.NewGuid(),
            ownerId: Guid.NewGuid());
        campaign.Activate(); // Campaign is active

        var command = new RegisterCommand(
            Email: "player@example.com",
            Password: "Password123",
            DisplayName: "Player User",
            Role: "Player",
            InviteCode: campaign.JoinCode);

        _userRepositoryMock
            .Setup(x => x.ExistsByEmailAsync(command.Email, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        _campaignRepositoryMock
            .Setup(x => x.GetByJoinCodeAsync(command.InviteCode!, It.IsAny<CancellationToken>()))
            .ReturnsAsync(campaign);

        _campaignMemberRepositoryMock
            .Setup(x => x.GetByCampaignAndUserAsync(campaign.Id, It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((CampaignMember?)null);

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

        _campaignMemberRepositoryMock.Verify(
            x => x.AddAsync(It.IsAny<CampaignMember>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WithValidPlayerDataWithoutInviteCode_ShouldRegisterPlayerOnly()
    {
        // Arrange
        var command = new RegisterCommand(
            Email: "player@example.com",
            Password: "Password123",
            DisplayName: "Player User",
            Role: "Player",
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
        result.Role.Should().Be("Player");

        // Should not attempt to join any campaign
        _campaignRepositoryMock.Verify(
            x => x.GetByJoinCodeAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
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
    public async Task Handle_WithInvalidInviteCode_ShouldThrowNotFoundException()
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

        _campaignRepositoryMock
            .Setup(x => x.GetByJoinCodeAsync(command.InviteCode!, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Campaign?)null);

        _passwordHasherMock
            .Setup(x => x.HashPassword(command.Password))
            .Returns("hashed_password");

        _jwtTokenGeneratorMock
            .Setup(x => x.GenerateRefreshToken())
            .Returns("refresh_token");

        _jwtTokenGeneratorMock
            .Setup(x => x.GetRefreshTokenExpiryTime())
            .Returns(DateTime.UtcNow.AddDays(7));

        // Act
        var act = () => _handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<NotFoundException>()
            .WithMessage("*Campaign with join code*");
    }

    [Fact]
    public async Task Handle_WithInactiveCampaign_ShouldThrowDomainException()
    {
        // Arrange
        var campaign = Campaign.Create(
            name: "Inactive Campaign",
            gameSystemId: Guid.NewGuid(),
            ownerId: Guid.NewGuid());
        campaign.Deactivate(); // Set as inactive
        campaign.IsActive.Should().BeFalse();

        var command = new RegisterCommand(
            Email: "player@example.com",
            Password: "Password123",
            DisplayName: "Player",
            Role: "Player",
            InviteCode: campaign.JoinCode);

        _userRepositoryMock
            .Setup(x => x.ExistsByEmailAsync(command.Email, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        _campaignRepositoryMock
            .Setup(x => x.GetByJoinCodeAsync(command.InviteCode!, It.IsAny<CancellationToken>()))
            .ReturnsAsync(campaign);

        _passwordHasherMock
            .Setup(x => x.HashPassword(command.Password))
            .Returns("hashed_password");

        _jwtTokenGeneratorMock
            .Setup(x => x.GenerateRefreshToken())
            .Returns("refresh_token");

        _jwtTokenGeneratorMock
            .Setup(x => x.GetRefreshTokenExpiryTime())
            .Returns(DateTime.UtcNow.AddDays(7));

        // Act
        var act = () => _handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<DomainException>()
            .WithMessage("*no longer accepting new members*");
    }

    #endregion
}
