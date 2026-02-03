using Loremaster.Domain.Entities;
using Loremaster.Domain.ValueObjects;

namespace Loremaster.Application.Common.Interfaces;

/// <summary>
/// Service interface for RAG-assisted entity generation.
/// Generates entity field values based on game system manuals and templates.
/// </summary>
public interface IEntityGenerationService
{
    /// <summary>
    /// Generates entity field values using RAG and the confirmed template schema.
    /// </summary>
    /// <param name="config">Generation configuration including template and context.</param>
    /// <param name="template">The confirmed entity template defining fields.</param>
    /// <param name="ownerId">The owner ID for scoping RAG queries.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Generation result with field values or error.</returns>
    Task<EntityGenerationResult> GenerateEntityFieldsAsync(
        EntityGenerationConfig config,
        EntityTemplate template,
        Guid ownerId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Generates an image/avatar for an entity based on its data.
    /// </summary>
    /// <param name="entity">The entity to generate an image for.</param>
    /// <param name="template">The entity template for context.</param>
    /// <param name="style">Optional style hint (fantasy, realistic, anime).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Image generation result with base64 data or error.</returns>
    Task<EntityImageGenerationResult> GenerateEntityImageAsync(
        LoreEntity entity,
        EntityTemplate template,
        string? style = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Regenerates an image for an existing entity.
    /// </summary>
    /// <param name="entity">The entity to regenerate image for.</param>
    /// <param name="template">The entity template for context.</param>
    /// <param name="style">Optional style hint.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Image generation result with base64 data or error.</returns>
    Task<EntityImageGenerationResult> RegenerateEntityImageAsync(
        LoreEntity entity,
        EntityTemplate template,
        string? style = null,
        CancellationToken cancellationToken = default);
}
