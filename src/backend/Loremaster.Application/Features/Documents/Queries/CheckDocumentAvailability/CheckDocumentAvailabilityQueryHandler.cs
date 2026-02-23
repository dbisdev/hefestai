using Loremaster.Application.Common.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Loremaster.Application.Features.Documents.Queries.CheckDocumentAvailability;

/// <summary>
/// Handler for CheckDocumentAvailabilityQuery.
/// Checks if a game system has documents available for RAG search.
/// Considers Admin-shared documents (globally accessible) and optionally the owner's documents.
/// </summary>
public class CheckDocumentAvailabilityQueryHandler 
    : IRequestHandler<CheckDocumentAvailabilityQuery, DocumentAvailabilityResult>
{
    private readonly IDocumentRepository _documentRepository;
    private readonly ILogger<CheckDocumentAvailabilityQueryHandler> _logger;

    public CheckDocumentAvailabilityQueryHandler(
        IDocumentRepository documentRepository,
        ILogger<CheckDocumentAvailabilityQueryHandler> logger)
    {
        _documentRepository = documentRepository;
        _logger = logger;
    }

    /// <summary>
    /// Handles the query by checking if documents are available for the game system.
    /// </summary>
    public async Task<DocumentAvailabilityResult> Handle(
        CheckDocumentAvailabilityQuery request, 
        CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Checking document availability for game system {GameSystemId}, ownerId: {OwnerId}, includeAdminDocs: {IncludeAdminDocs}, includeAllDocs: {IncludeAllDocs}",
            request.GameSystemId, request.OwnerId, request.IncludeAdminDocs, request.IncludeAllDocs);

        var hasDocuments = await _documentRepository.HasDocumentsForGameSystemAsync(
            request.GameSystemId,
            request.OwnerId,
            request.IncludeAdminDocs,
            request.IncludeAllDocs,
            cancellationToken);

        _logger.LogInformation(
            "Document availability for game system {GameSystemId}: {HasDocuments}",
            request.GameSystemId, hasDocuments);

        return new DocumentAvailabilityResult(hasDocuments, request.GameSystemId);
    }
}
