using Loremaster.Application.Common.Interfaces;
using Loremaster.Domain.Entities;
using Loremaster.Domain.ValueObjects;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Loremaster.Application.Features.EntityTemplates.Commands.CreateTemplate;

/// <summary>
/// Handler for CreateTemplateCommand.
/// Creates a new entity template in Draft status.
/// </summary>
public class CreateTemplateCommandHandler : IRequestHandler<CreateTemplateCommand, CreateTemplateResult>
{
    private readonly IEntityTemplateRepository _templateRepository;
    private readonly IGameSystemRepository _gameSystemRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<CreateTemplateCommandHandler> _logger;

    public CreateTemplateCommandHandler(
        IEntityTemplateRepository templateRepository,
        IGameSystemRepository gameSystemRepository,
        IUnitOfWork unitOfWork,
        ILogger<CreateTemplateCommandHandler> logger)
    {
        _templateRepository = templateRepository;
        _gameSystemRepository = gameSystemRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    /// <summary>
    /// Handles the create template command.
    /// </summary>
    /// <param name="request">The create template request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The result containing the new template's details.</returns>
    /// <exception cref="ArgumentException">Thrown when game system not found or template already exists.</exception>
    public async Task<CreateTemplateResult> Handle(
        CreateTemplateCommand request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Creating template '{EntityTypeName}' for game system {GameSystemId} by user {UserId}",
            request.EntityTypeName, request.GameSystemId, request.OwnerId);

        // Validate game system exists
        var gameSystem = await _gameSystemRepository.GetByIdAsync(request.GameSystemId, cancellationToken);
        if (gameSystem == null)
        {
            throw new ArgumentException($"Game system with ID {request.GameSystemId} not found");
        }

        // Check for duplicate entity type name in this game system for this owner
        var normalizedTypeName = EntityTemplate.NormalizeEntityTypeName(request.EntityTypeName);
        var exists = await _templateRepository.ExistsByEntityTypeNameAsync(
            request.GameSystemId,
            request.OwnerId,
            normalizedTypeName,
            cancellationToken);

        if (exists)
        {
            throw new ArgumentException($"A template with entity type '{normalizedTypeName}' already exists for this game system");
        }

        // Create the template
        var template = EntityTemplate.Create(
            request.EntityTypeName,
            request.DisplayName,
            request.GameSystemId,
            request.OwnerId,
            request.Description,
            sourceDocumentId: null, // Manual creation has no source document
            request.Version,
            request.IconHint);

        // Add field definitions if provided
        if (request.Fields != null && request.Fields.Count > 0)
        {
            var fields = request.Fields.Select(f => FieldDefinition.Create(
                f.Name,
                f.DisplayName,
                f.FieldType,
                f.IsRequired,
                f.DefaultValue,
                f.Description,
                f.Order,
                f.Options,
                f.MinValue,
                f.MaxValue,
                f.ValidationPattern
            )).ToList();

            template.SetFieldDefinitions(fields);
        }

        await _templateRepository.AddAsync(template, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Created template {TemplateId} '{EntityTypeName}' with {FieldCount} fields",
            template.Id, template.EntityTypeName, template.GetFieldDefinitions().Count);

        return new CreateTemplateResult(
            template.Id,
            template.EntityTypeName,
            template.DisplayName,
            template.GetFieldDefinitions().Count,
            template.CreatedAt);
    }
}
