using MediatR;

namespace Loremaster.Application.Features.Documents.Queries.CheckDocumentAvailability;

/// <summary>
/// Query to check if a game system has documents available for semantic search (RAG).
/// Accessible by Players - considers Admin-shared documents and optionally Master's own documents.
/// </summary>
/// <param name="GameSystemId">The game system ID to check for documents.</param>
/// <param name="OwnerId">Optional owner ID to include their documents (for Masters).</param>
/// <param name="IncludeAdminDocs">When true, includes documents owned by Admin users (shared docs).</param>
/// <param name="IncludeAllDocs">When true, includes all documents regardless of owner (for Admins).</param>
public record CheckDocumentAvailabilityQuery(
    Guid GameSystemId,
    Guid? OwnerId,
    bool IncludeAdminDocs = true,
    bool IncludeAllDocs = false) : IRequest<DocumentAvailabilityResult>;

/// <summary>
/// Result of document availability check.
/// </summary>
/// <param name="HasDocuments">Whether documents are available for RAG search.</param>
/// <param name="GameSystemId">The game system ID that was checked.</param>
public record DocumentAvailabilityResult(
    bool HasDocuments,
    Guid GameSystemId);
