using Loremaster.Application.Common.Interfaces;
using Loremaster.Application.Features.GameSystems.DTOs;
using Loremaster.Domain.Exceptions;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Loremaster.Application.Features.GameSystems.Commands.UpdateGameSystem;

/// <summary>
/// Handler for UpdateGameSystemCommand. Updates an existing game system.
/// Only users with Admin role can update game systems.
/// </summary>
public class UpdateGameSystemCommandHandler : IRequestHandler<UpdateGameSystemCommand, GameSystemDto>
{
    private readonly IGameSystemRepository _gameSystemRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<UpdateGameSystemCommandHandler> _logger;

    public UpdateGameSystemCommandHandler(
        IGameSystemRepository gameSystemRepository,
        ICurrentUserService currentUserService,
        IUnitOfWork unitOfWork,
        ILogger<UpdateGameSystemCommandHandler> logger)
    {
        _gameSystemRepository = gameSystemRepository;
        _currentUserService = currentUserService;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    /// <summary>
    /// Handles the update game system command.
    /// </summary>
    /// <param name="request">The update command.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The updated game system as a DTO.</returns>
    /// <exception cref="NotFoundException">Thrown when game system not found.</exception>
    public async Task<GameSystemDto> Handle(
        UpdateGameSystemCommand request, 
        CancellationToken cancellationToken)
    {
        var gameSystem = await _gameSystemRepository.GetByIdAsync(request.Id, cancellationToken);
        
        if (gameSystem == null)
        {
            throw new NotFoundException("GameSystem", request.Id);
        }

        // Update the game system
        gameSystem.Update(
            name: request.Name,
            publisher: request.Publisher,
            version: request.Version,
            description: request.Description,
            supportedEntityTypes: request.SupportedEntityTypes
        );

        _gameSystemRepository.Update(gameSystem);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "GameSystem {Code} updated by user {UserId}", 
            gameSystem.Code, _currentUserService.UserId);

        return GameSystemDto.FromEntity(gameSystem);
    }
}
