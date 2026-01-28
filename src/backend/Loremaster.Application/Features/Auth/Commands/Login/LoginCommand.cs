using MediatR;

namespace Loremaster.Application.Features.Auth.Commands.Login;

public record LoginCommand(
    string Email,
    string Password
) : IRequest<LoginResponse>;
