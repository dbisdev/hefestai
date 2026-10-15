using Loremaster.Application.Common.Interfaces;
using Loremaster.Domain.Exceptions;
using MediatR;

namespace Loremaster.Application.Features.Auth.Commands.RefreshToken;

/// <summary>
/// Handles the RefreshToken command.
/// Validates the provided refresh token, rotates it, and issues a new access token.
/// </summary>
public class RefreshTokenCommandHandler : IRequestHandler<RefreshTokenCommand, RefreshTokenResponse>
{
    private readonly IUserRepository _userRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IJwtTokenGenerator _jwtTokenGenerator;
    private readonly IDateTimeProvider _dateTimeProvider;

    public RefreshTokenCommandHandler(
        IUserRepository userRepository,
        IUnitOfWork unitOfWork,
        IJwtTokenGenerator jwtTokenGenerator,
        IDateTimeProvider dateTimeProvider)
    {
        _userRepository = userRepository;
        _unitOfWork = unitOfWork;
        _jwtTokenGenerator = jwtTokenGenerator;
        _dateTimeProvider = dateTimeProvider;
    }

    /// <summary>
    /// Validates the refresh token, rotates it, and returns a new access/refresh token pair.
    /// </summary>
    public async Task<RefreshTokenResponse> Handle(RefreshTokenCommand request, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByRefreshTokenAsync(request.RefreshToken, cancellationToken);

        if (user == null)
            throw new DomainException("Invalid refresh token");

        if (user.RefreshTokenExpiryTime < _dateTimeProvider.UtcNow)
            throw new DomainException("Refresh token has expired");

        if (!user.IsActive)
            throw new ForbiddenAccessException("This account has been deactivated");

        var newRefreshToken = _jwtTokenGenerator.GenerateRefreshToken();
        var refreshTokenExpiry = _jwtTokenGenerator.GetRefreshTokenExpiryTime();
        user.SetRefreshToken(newRefreshToken, refreshTokenExpiry);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var accessToken = _jwtTokenGenerator.GenerateAccessToken(user);

        return new RefreshTokenResponse(accessToken, newRefreshToken);
    }
}
