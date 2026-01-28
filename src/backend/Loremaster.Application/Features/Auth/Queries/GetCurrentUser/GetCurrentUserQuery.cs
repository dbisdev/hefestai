using MediatR;

namespace Loremaster.Application.Features.Auth.Queries.GetCurrentUser;

public record GetCurrentUserQuery : IRequest<CurrentUserResponse>;
