using Loremaster.Application.Common.Interfaces;
using Loremaster.Domain.Exceptions;
using MediatR;

namespace Loremaster.Application.Features.Auth.Commands.Login;

public class LoginCommandHandler : IRequestHandler<LoginCommand, LoginResponse>
{
    private readonly IUserRepository _userRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtTokenGenerator _jwtTokenGenerator;

    public LoginCommandHandler(
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

    public async Task<LoginResponse> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByEmailAsync(request.Email, cancellationToken);
        
        if (user == null || !_passwordHasher.VerifyPassword(request.Password, user.PasswordHash))
        {
            throw new DomainException("Invalid email or password");
        }

        if (!user.IsActive)
        {
            throw new ForbiddenAccessException("This account has been deactivated");
        }

        var refreshToken = _jwtTokenGenerator.GenerateRefreshToken();
        var refreshTokenExpiry = _jwtTokenGenerator.GetRefreshTokenExpiryTime();
        user.SetRefreshToken(refreshToken, refreshTokenExpiry);
        user.RecordLogin();

        await _userRepository.UpdateAsync(user, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var accessToken = _jwtTokenGenerator.GenerateAccessToken(user);

        return new LoginResponse(
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
