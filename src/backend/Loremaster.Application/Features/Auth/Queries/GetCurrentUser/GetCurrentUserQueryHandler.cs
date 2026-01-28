using Loremaster.Application.Common.Interfaces;
using Loremaster.Domain.Exceptions;
using MediatR;

namespace Loremaster.Application.Features.Auth.Queries.GetCurrentUser;

public class GetCurrentUserQueryHandler : IRequestHandler<GetCurrentUserQuery, CurrentUserResponse>
{
    private readonly IUserRepository _userRepository;
    private readonly ICurrentUserService _currentUserService;

    public GetCurrentUserQueryHandler(
        IUserRepository userRepository,
        ICurrentUserService currentUserService)
    {
        _userRepository = userRepository;
        _currentUserService = currentUserService;
    }

    public async Task<CurrentUserResponse> Handle(GetCurrentUserQuery request, CancellationToken cancellationToken)
    {
        if (_currentUserService.UserId == null)
        {
            throw new ForbiddenAccessException("User is not authenticated");
        }

        var userId = _currentUserService.UserId.Value;
        var user = await _userRepository.GetByIdAsync(userId, cancellationToken);

        if (user == null)
        {
            throw new NotFoundException("User", userId);
        }

        return new CurrentUserResponse(
            user.Id,
            user.Email,
            user.DisplayName,
            user.Role.ToString(),
            user.CreatedAt,
            user.LastLoginAt,
            user.InvitationCode,
            user.MasterId?.ToString()
        );
    }
}
