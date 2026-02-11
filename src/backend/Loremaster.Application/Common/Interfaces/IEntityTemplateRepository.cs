using Loremaster.Domain.Entities;
using Loremaster.Domain.Enums;

namespace Loremaster.Application.Common.Interfaces;

/// <summary>
/// Repository interface for EntityTemplate entity.
/// Manages template CRUD operations and queries.
/// </summary>
public interface IEntityTemplateRepository
{
    /// <summary>
    /// Gets a template by ID.
    /// </summary>
    Task<EntityTemplate?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets a template by ID with ownership check.
    /// </summary>
    Task<EntityTemplate?> GetByIdAsync(Guid id, Guid ownerId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets all templates for a game system.
    /// </summary>
    Task<IReadOnlyList<EntityTemplate>> GetByGameSystemIdAsync(
        Guid gameSystemId, 
        Guid ownerId,
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets all confirmed templates for a game system.
    /// </summary>
    Task<IReadOnlyList<EntityTemplate>> GetConfirmedByGameSystemIdAsync(
        Guid gameSystemId, 
        Guid ownerId,
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets all confirmed templates for a game system, regardless of owner.
    /// Used for displaying available templates to all Masters using campaigns with this game system.
    /// </summary>
    Task<IReadOnlyList<EntityTemplate>> GetAllConfirmedByGameSystemIdAsync(
        Guid gameSystemId,
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets templates by status for a game system.
    /// </summary>
    Task<IReadOnlyList<EntityTemplate>> GetByStatusAsync(
        Guid gameSystemId, 
        Guid ownerId,
        TemplateStatus status,
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets a confirmed template for a specific entity type within a game system.
    /// Used to validate entity creation.
    /// </summary>
    Task<EntityTemplate?> GetConfirmedTemplateForEntityTypeAsync(
        Guid gameSystemId,
        Guid ownerId,
        string entityTypeName,
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Checks if a confirmed template exists for the entity type.
    /// </summary>
    Task<bool> HasConfirmedTemplateAsync(
        Guid gameSystemId,
        Guid ownerId,
        string entityTypeName,
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Checks if a template with the same entity type name already exists for the game system.
    /// </summary>
    Task<bool> ExistsByEntityTypeNameAsync(
        Guid gameSystemId,
        Guid ownerId,
        string entityTypeName,
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Adds a new template.
    /// </summary>
    Task AddAsync(EntityTemplate template, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Adds multiple templates.
    /// </summary>
    Task AddRangeAsync(IEnumerable<EntityTemplate> templates, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Updates an existing template.
    /// </summary>
    void Update(EntityTemplate template);
    
    /// <summary>
    /// Deletes a template.
    /// </summary>
    void Delete(EntityTemplate template);
}
