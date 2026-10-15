using Loremaster.Application.Common.Interfaces;
using Loremaster.Domain.Entities;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Loremaster.Application.Features.Documents.Commands.IngestDocument;

/// <summary>
/// Handles the IngestDocument command.
/// <para>
/// ACID guarantee: the document row is persisted first (first <c>SaveChangesAsync</c>),
/// then the embedding is generated via the external API. A second <c>SaveChangesAsync</c>
/// updates the embedding. This ensures that a failure during the external call does not
/// leave an unpersisted entity in an ambiguous state, and avoids consuming embedding
/// API quota before the document is durably stored.
/// If embedding generation fails, the document is retained without an embedding
/// (non-blocking) and can be re-embedded later via the dedicated endpoint.
/// </para>
/// </summary>
public class IngestDocumentCommandHandler : IRequestHandler<IngestDocumentCommand, IngestDocumentResult>
{
    private readonly IDocumentRepository _documentRepository;
    private readonly IEmbeddingService _embeddingService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<IngestDocumentCommandHandler> _logger;

    public IngestDocumentCommandHandler(
        IDocumentRepository documentRepository,
        IEmbeddingService embeddingService,
        IUnitOfWork unitOfWork,
        ILogger<IngestDocumentCommandHandler> logger)
    {
        _documentRepository = documentRepository;
        _embeddingService = embeddingService;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    /// <summary>
    /// Ingests a document, persists it durably, then generates and attaches an embedding
    /// in a separate transaction step.
    /// </summary>
    public async Task<IngestDocumentResult> Handle(IngestDocumentCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Ingesting document: {Title} for owner {OwnerId}", request.Title, request.OwnerId);

        // Step 1: Create and persist the document before calling any external service.
        // This guarantees the document row exists in the DB regardless of what happens next.
        var document = Document.Create(
            request.Title,
            request.Content,
            request.OwnerId,
            request.Source,
            request.Metadata);

        await _documentRepository.AddAsync(document, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogDebug("Document {DocumentId} persisted. Proceeding to embedding generation.", document.Id);

        bool embeddingGenerated = false;
        int? embeddingDimensions = null;

        // Step 2: Generate the embedding only after the document is durably stored.
        // Failure here is non-blocking: the document is retained and can be re-embedded later.
        if (request.GenerateEmbedding)
        {
            try
            {
                _logger.LogDebug("Generating embedding for document {DocumentId}", document.Id);

                var embedding = await _embeddingService.GetEmbeddingAsync(request.Content, cancellationToken);
                document.SetEmbedding(embedding);

                embeddingGenerated = true;
                embeddingDimensions = embedding.Length;

                // Persist the embedding update as a second transaction step.
                await _documentRepository.UpdateAsync(document, cancellationToken);
                await _unitOfWork.SaveChangesAsync(cancellationToken);

                _logger.LogInformation(
                    "Embedding generated for document {DocumentId}, dimensions: {Dimensions}",
                    document.Id, embeddingDimensions);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(
                    ex,
                    "Failed to generate embedding for document {DocumentId}. " +
                    "Document is saved without embedding and can be re-embedded later.",
                    document.Id);
            }
        }

        return new IngestDocumentResult(document.Id, embeddingGenerated, embeddingDimensions);
    }
}
