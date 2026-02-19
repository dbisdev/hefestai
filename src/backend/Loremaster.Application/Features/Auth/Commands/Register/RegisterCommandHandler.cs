using Loremaster.Application.Common.Interfaces;
using Loremaster.Domain.Entities;
using Loremaster.Domain.Enums;
using Loremaster.Domain.Exceptions;
using MediatR;

namespace Loremaster.Application.Features.Auth.Commands.Register;

/// <summary>
/// Handler for user registration.
/// Users can register without an invitation code and optionally join a campaign during registration.
/// </summary>
public class RegisterCommandHandler : IRequestHandler<RegisterCommand, RegisterResponse>
{
    private readonly IUserRepository _userRepository;
    private readonly ICampaignRepository _campaignRepository;
    private readonly ICampaignMemberRepository _campaignMemberRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtTokenGenerator _jwtTokenGenerator;

    public RegisterCommandHandler(
        IUserRepository userRepository,
        ICampaignRepository campaignRepository,
        ICampaignMemberRepository campaignMemberRepository,
        IUnitOfWork unitOfWork,
        IPasswordHasher passwordHasher,
        IJwtTokenGenerator jwtTokenGenerator)
    {
        _userRepository = userRepository;
        _campaignRepository = campaignRepository;
        _campaignMemberRepository = campaignMemberRepository;
        _unitOfWork = unitOfWork;
        _passwordHasher = passwordHasher;
        _jwtTokenGenerator = jwtTokenGenerator;
    }

    public async Task<RegisterResponse> Handle(RegisterCommand request, CancellationToken cancellationToken)
    {
        var emailExists = await _userRepository.ExistsByEmailAsync(request.Email, cancellationToken);
        if (emailExists)
        {
            throw new DomainException("A user with this email already exists");
        }

        // Parse role, default to Player
        var role = UserRole.Player;
        if (!string.IsNullOrEmpty(request.Role))
        {
            if (!Enum.TryParse<UserRole>(request.Role, true, out role))
            {
                throw new DomainException("Invalid role specified");
            }
        }

        var passwordHash = _passwordHasher.HashPassword(request.Password);
        var user = User.Create(request.Email, passwordHash, request.DisplayName, role);

        var refreshToken = _jwtTokenGenerator.GenerateRefreshToken();
        var refreshTokenExpiry = _jwtTokenGenerator.GetRefreshTokenExpiryTime();
        user.SetRefreshToken(refreshToken, refreshTokenExpiry);
        user.RecordLogin();

        await _userRepository.AddAsync(user, cancellationToken);

        // If invite code is provided, join the user to the campaign
        if (!string.IsNullOrEmpty(request.InviteCode))
        {
            await JoinCampaignAsync(user, request.InviteCode, cancellationToken);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var accessToken = _jwtTokenGenerator.GenerateAccessToken(user);

        return new RegisterResponse(
            user.Id,
            user.Email,
            user.DisplayName,
            user.Role.ToString(),
            accessToken,
            refreshToken
        );
    }

    /// <summary>
    /// Joins the user to a campaign using the provided join code.
    /// </summary>
    private async Task JoinCampaignAsync(User user, string joinCode, CancellationToken cancellationToken)
    {
        var campaign = await _campaignRepository.GetByJoinCodeAsync(joinCode, cancellationToken);
        if (campaign == null)
        {
            throw new NotFoundException("Campaign with join code", joinCode);
        }

        if (!campaign.IsActive)
        {
            throw new DomainException("This campaign is no longer accepting new members");
        }

        // Check if already a member (should not happen for new users, but good to check)
        var existingMembership = await _campaignMemberRepository
            .GetByCampaignAndUserAsync(campaign.Id, user.Id, cancellationToken);
        if (existingMembership != null)
        {
            throw new DomainException("You are already a member of this campaign");
        }

        // Join as Player
        var membership = CampaignMember.Create(campaign.Id, user.Id, CampaignRole.Player);
        await _campaignMemberRepository.AddAsync(membership, cancellationToken);
    }
}
