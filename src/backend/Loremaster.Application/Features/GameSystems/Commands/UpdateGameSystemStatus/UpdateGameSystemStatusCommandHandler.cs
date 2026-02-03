using Loremaster.Application.Common.Interfaces;
using Loremaster.Application.Features.GameSystems.DTOs;
using Loremaster.Domain.Exceptions;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Loremaster.Application.Features.GameSystems.Commands.UpdateGameSystemStatus;

/// <summary>
/// Handler for UpdateGameSystemStatusCommand. Activates or deactivates a game system.
/// Only users with Admin role can change game system status.
/// </summary>
public class UpdateGameSystemStatusCommandHandler : IRequestHandler<UpdateGameSystemStatusCommand, GameSystemDto>
{
    private readonly IGameSystemRepository _gameSystemRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<UpdateGameSystemStatusCommandHandler> _logger;

    public UpdateGameSystemStatusCommandHandler(
        IGameSystemRepository gameSystemRepository,
        ICurrentUserService currentUserService,
        IUnitOfWork unitOfWork,
        ILogger<UpdateGameSystemStatusCommandHandler> logger)
    {
        _gameSystemRepository = gameSystemRepository;
        _currentUserService = currentUserService;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    /// <summary>
    /// Handles the update game system status command.
    /// </summary>
    /// <param name="request">The status update command.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The updated game system as a DTO.</returns>
    /// <exception cref="NotFoundException">Thrown when game system not found.</exception>
    public async Task<GameSystemDto> Handle(
        UpdateGameSystemStatusCommand request, 
        CancellationToken cancellationToken)
    {
        var gameSystem = await _gameSystemRepository.GetByIdAsync(request.Id, cancellationToken);
        
        if (gameSystem == null)
        {
            throw new NotFoundException("GameSystem", request.Id);
        }

        // Update status
        if (request.IsActive)
        {
            gameSystem.Activate();
        }
        else
        {
            gameSystem.Deactivate();
        }

        _gameSystemRepository.Update(gameSystem);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "GameSystem {Code} status changed to {IsActive} by user {UserId}", 
            gameSystem.Code, request.IsActive, _currentUserService.UserId);

        return GameSystemDto.FromEntity(gameSystem);
    }
}
