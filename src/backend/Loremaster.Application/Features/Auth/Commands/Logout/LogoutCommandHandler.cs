using Loremaster.Application.Common.Interfaces;
using MediatR;

namespace Loremaster.Application.Features.Auth.Commands.Logout;

public class LogoutCommandHandler : IRequestHandler<LogoutCommand, Unit>
{
    private readonly ICurrentUserService _currentUserService;
    private readonly IUserRepository _userRepository;
    private readonly IUnitOfWork _unitOfWork;

    public LogoutCommandHandler(
        ICurrentUserService currentUserService,
        IUserRepository userRepository,
        IUnitOfWork unitOfWork)
    {
        _currentUserService = currentUserService;
        _userRepository = userRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Unit> Handle(LogoutCommand request, CancellationToken cancellationToken)
    {
        if (_currentUserService.UserId.HasValue)
        {
            var userId = _currentUserService.UserId.Value;
            var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
            if (user != null)
            {
                user.RevokeRefreshToken();
                await _userRepository.UpdateAsync(user, cancellationToken);
                await _unitOfWork.SaveChangesAsync(cancellationToken);
            }
        }

        return Unit.Value;
    }
}
