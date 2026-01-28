using Loremaster.Application.Common.Interfaces;
using Loremaster.Domain.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Loremaster.Application.Features.Auth.Commands.RefreshToken;

public class RefreshTokenCommandHandler : IRequestHandler<RefreshTokenCommand, RefreshTokenResponse>
{
    private readonly IApplicationDbContext _context;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IJwtTokenGenerator _jwtTokenGenerator;
    private readonly IDateTimeProvider _dateTimeProvider;

    public RefreshTokenCommandHandler(
        IApplicationDbContext context,
        IUnitOfWork unitOfWork,
        IJwtTokenGenerator jwtTokenGenerator,
        IDateTimeProvider dateTimeProvider)
    {
        _context = context;
        _unitOfWork = unitOfWork;
        _jwtTokenGenerator = jwtTokenGenerator;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<RefreshTokenResponse> Handle(RefreshTokenCommand request, CancellationToken cancellationToken)
    {
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.RefreshToken == request.RefreshToken, cancellationToken);

        if (user == null)
        {
            throw new DomainException("Invalid refresh token");
        }

        if (user.RefreshTokenExpiryTime < _dateTimeProvider.UtcNow)
        {
            throw new DomainException("Refresh token has expired");
        }

        if (!user.IsActive)
        {
            throw new ForbiddenAccessException("This account has been deactivated");
        }

        var newRefreshToken = _jwtTokenGenerator.GenerateRefreshToken();
        var refreshTokenExpiry = _jwtTokenGenerator.GetRefreshTokenExpiryTime();
        user.SetRefreshToken(newRefreshToken, refreshTokenExpiry);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var accessToken = _jwtTokenGenerator.GenerateAccessToken(user);

        return new RefreshTokenResponse(accessToken, newRefreshToken);
    }
}
