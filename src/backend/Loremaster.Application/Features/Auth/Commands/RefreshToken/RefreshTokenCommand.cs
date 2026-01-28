using MediatR;

namespace Loremaster.Application.Features.Auth.Commands.RefreshToken;

public record RefreshTokenCommand(
    string RefreshToken
) : IRequest<RefreshTokenResponse>;
