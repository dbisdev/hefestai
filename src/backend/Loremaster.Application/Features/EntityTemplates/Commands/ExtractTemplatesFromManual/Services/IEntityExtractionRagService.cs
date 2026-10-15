using Loremaster.Application.Common.Interfaces;

namespace Loremaster.Application.Features.EntityTemplates.Commands.ExtractTemplatesFromManual.Services;

/// <summary>
/// Responsible for running semantic searches against game-manual documents and building
/// a ranked context list for LLM-based entity-type extraction.
/// </summary>
internal interface IEntityExtractionRagService
{
    /// <summary>
    /// Performs multiple semantic searches (one per built-in entity-category query),
    /// deduplicates results by document ID, and returns up to <paramref name="maxContextItems"/>
    /// top-ranked content chunks together with the ID of the first document found.
    /// </summary>
    /// <param name="gameSystemId">Game system whose documents are searched.</param>
    /// <param name="userId">Requesting user (used for ownership filtering).</param>
    /// <param name="isAdminOrOwner">When <see langword="true"/> the owner filter is skipped.</param>
    /// <param name="maxContextItems">Maximum number of context chunks to return (default 10).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>
    /// A tuple of (context chunks ordered by similarity desc, ID of the first document found).
    /// Returns an empty list when no documents exist for the game system.
    /// </returns>
    Task<(IReadOnlyList<string> ContextChunks, Guid? FirstDocumentId)> GetExtractionContextAsync(
        Guid gameSystemId,
        Guid userId,
        bool isAdminOrOwner,
        int maxContextItems = 10,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Calls the embedding/RAG service with the pre-built context and the standard
    /// entity-type extraction prompt, returning the raw LLM answer string.
    /// </summary>
    Task<string> GenerateEntityTypesAsync(
        IReadOnlyList<string> contextChunks,
        CancellationToken cancellationToken = default);
}
