using Loremaster.Application.Common.Interfaces;
using Loremaster.Domain.Entities;
using Loremaster.Domain.Enums;
using Loremaster.Domain.Exceptions;
using MediatR;

namespace Loremaster.Application.Features.Auth.Commands.Register;

/// <summary>
/// Handler for user registration.
/// Users can register without an invitation code and optionally join a campaign during registration.
/// <para>
/// ACID ordering: all validation is performed before any entities are staged in the EF change
/// tracker. Once validation passes, <c>AddAsync</c> calls stage both the user and (optionally)
/// the campaign membership, and a single <c>SaveChangesAsync</c> commits them atomically.
/// This prevents partial state where a campaign lookup failure would occur after user staging,
/// making the intent explicit and the flow easier to reason about.
/// </para>
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

    /// <summary>
    /// Registers a new user and optionally enrolls them in a campaign.
    /// All validation runs before any entity is staged to keep the EF change tracker clean
    /// on failure paths.
    /// </summary>
    public async Task<RegisterResponse> Handle(RegisterCommand request, CancellationToken cancellationToken)
    {
        // --- Phase 1: Validate all inputs before staging any entities ---

        var emailExists = await _userRepository.ExistsByEmailAsync(request.Email, cancellationToken);
        if (emailExists)
            throw new DomainException("A user with this email already exists");

        // Parse role, default to Player
        var role = UserRole.Player;
        if (!string.IsNullOrEmpty(request.Role))
        {
            if (!Enum.TryParse<UserRole>(request.Role, true, out role))
                throw new DomainException("Invalid role specified");
        }

        // Validate campaign invite code up-front (before creating the user entity)
        Campaign? campaignToJoin = null;
        if (!string.IsNullOrEmpty(request.InviteCode))
        {
            campaignToJoin = await _campaignRepository.GetByJoinCodeAsync(request.InviteCode, cancellationToken);
            if (campaignToJoin == null)
                throw new NotFoundException("Campaign with join code", request.InviteCode);

            if (!campaignToJoin.IsActive)
                throw new DomainException("This campaign is no longer accepting new members");
        }

        // --- Phase 2: All validation passed — stage and persist ---

        var passwordHash = _passwordHasher.HashPassword(request.Password);
        var user = User.Create(request.Email, passwordHash, request.DisplayName, role);

        var refreshToken = _jwtTokenGenerator.GenerateRefreshToken();
        var refreshTokenExpiry = _jwtTokenGenerator.GetRefreshTokenExpiryTime();
        user.SetRefreshToken(refreshToken, refreshTokenExpiry);
        user.RecordLogin();

        await _userRepository.AddAsync(user, cancellationToken);

        // Stage campaign membership if an invite code was provided and validated
        if (campaignToJoin != null)
        {
            var membership = CampaignMember.Create(campaignToJoin.Id, user.Id, CampaignRole.Player);
            await _campaignMemberRepository.AddAsync(membership, cancellationToken);
        }

        // Atomic commit: user + optional membership in a single transaction
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
}
