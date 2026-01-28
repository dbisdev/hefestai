using Loremaster.Domain.Entities;

namespace Loremaster.Application.Common.Interfaces;

public interface IJwtTokenGenerator
{
    string GenerateAccessToken(User user);
    string GenerateRefreshToken();
    DateTime GetRefreshTokenExpiryTime();
}
