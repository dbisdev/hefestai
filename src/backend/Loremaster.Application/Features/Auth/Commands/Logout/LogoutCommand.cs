using MediatR;

namespace Loremaster.Application.Features.Auth.Commands.Logout;

public record LogoutCommand : IRequest<Unit>;
