using Loremaster.Application.Common.Interfaces;
using Loremaster.Application.Features.GameSystems.DTOs;
using Loremaster.Domain.Entities;
using Loremaster.Domain.Exceptions;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Loremaster.Application.Features.GameSystems.Commands.CreateGameSystem;

/// <summary>
/// Handler for CreateGameSystemCommand. Creates a new game system.
/// Only users with Admin role can create game systems.
/// </summary>
public class CreateGameSystemCommandHandler : IRequestHandler<CreateGameSystemCommand, GameSystemDto>
{
    private readonly IGameSystemRepository _gameSystemRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<CreateGameSystemCommandHandler> _logger;

    public CreateGameSystemCommandHandler(
        IGameSystemRepository gameSystemRepository,
        ICurrentUserService currentUserService,
        IUnitOfWork unitOfWork,
        ILogger<CreateGameSystemCommandHandler> logger)
    {
        _gameSystemRepository = gameSystemRepository;
        _currentUserService = currentUserService;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    /// <summary>
    /// Handles the create game system command.
    /// </summary>
    /// <param name="request">The create command.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The created game system as a DTO.</returns>
    /// <exception cref="DomainException">Thrown when a game system with the same code already exists.</exception>
    public async Task<GameSystemDto> Handle(
        CreateGameSystemCommand request, 
        CancellationToken cancellationToken)
    {
        // Check if code already exists
        var exists = await _gameSystemRepository.ExistsByCodeAsync(request.Code, cancellationToken);
        if (exists)
        {
            throw new DomainException($"A game system with code '{request.Code}' already exists");
        }

        // Create the game system
        var gameSystem = GameSystem.Create(
            code: request.Code,
            name: request.Name,
            publisher: request.Publisher,
            version: request.Version,
            description: request.Description,
            supportedEntityTypes: request.SupportedEntityTypes
        );

        await _gameSystemRepository.AddAsync(gameSystem, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "GameSystem {Code} created by user {UserId}", 
            gameSystem.Code, _currentUserService.UserId);

        return GameSystemDto.FromEntity(gameSystem);
    }
}
