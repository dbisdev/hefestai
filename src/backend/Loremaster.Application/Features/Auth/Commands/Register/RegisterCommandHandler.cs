using Loremaster.Application.Common.Interfaces;
using Loremaster.Domain.Entities;
using Loremaster.Domain.Enums;
using Loremaster.Domain.Exceptions;
using MediatR;

namespace Loremaster.Application.Features.Auth.Commands.Register;

public class RegisterCommandHandler : IRequestHandler<RegisterCommand, RegisterResponse>
{
    private readonly IUserRepository _userRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtTokenGenerator _jwtTokenGenerator;

    public RegisterCommandHandler(
        IUserRepository userRepository,
        IUnitOfWork unitOfWork,
        IPasswordHasher passwordHasher,
        IJwtTokenGenerator jwtTokenGenerator)
    {
        _userRepository = userRepository;
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

        Guid? masterId = null;

        // If registering as a Player with an invite code, find the Master
        if (role == UserRole.Player && !string.IsNullOrEmpty(request.InviteCode))
        {
            var master = await _userRepository.GetByInvitationCodeAsync(request.InviteCode, cancellationToken);
            if (master == null)
            {
                throw new DomainException("Invalid invitation code");
            }
            masterId = master.Id;
        }
        
        // Players must have a Master
        if (role == UserRole.Player && masterId == null)
        {
            throw new DomainException("Players must be associated with a Master. Please provide an invitation code.");
        }

        var passwordHash = _passwordHasher.HashPassword(request.Password);
        var user = User.Create(request.Email, passwordHash, request.DisplayName, role, masterId);

        var refreshToken = _jwtTokenGenerator.GenerateRefreshToken();
        var refreshTokenExpiry = _jwtTokenGenerator.GetRefreshTokenExpiryTime();
        user.SetRefreshToken(refreshToken, refreshTokenExpiry);
        user.RecordLogin();

        await _userRepository.AddAsync(user, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var accessToken = _jwtTokenGenerator.GenerateAccessToken(user);

        return new RegisterResponse(
            user.Id,
            user.Email,
            user.DisplayName,
            user.Role.ToString(),
            accessToken,
            refreshToken,
            user.InvitationCode,
            user.MasterId?.ToString()
        );
    }
}
