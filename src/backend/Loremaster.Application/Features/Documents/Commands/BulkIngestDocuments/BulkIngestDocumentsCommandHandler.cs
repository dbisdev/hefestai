using Loremaster.Application.Features.Documents.Commands.IngestDocument;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Loremaster.Application.Features.Documents.Commands.BulkIngestDocuments;

/// <summary>
/// Handles <see cref="BulkIngestDocumentsCommand"/> by dispatching an individual
/// <see cref="IngestDocumentCommand"/> for each document in the batch.
/// Failures are captured and reported rather than aborting the entire batch,
/// so a single bad document does not block the rest.
/// </summary>
public sealed class BulkIngestDocumentsCommandHandler
    : IRequestHandler<BulkIngestDocumentsCommand, BulkIngestDocumentsResult>
{
    private readonly IMediator _mediator;
    private readonly ILogger<BulkIngestDocumentsCommandHandler> _logger;

    /// <summary>
    /// Initializes a new instance of <see cref="BulkIngestDocumentsCommandHandler"/>.
    /// </summary>
    /// <param name="mediator">MediatR mediator used to dispatch per-document commands.</param>
    /// <param name="logger">Logger for diagnostic output.</param>
    public BulkIngestDocumentsCommandHandler(
        IMediator mediator,
        ILogger<BulkIngestDocumentsCommandHandler> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<BulkIngestDocumentsResult> Handle(
        BulkIngestDocumentsCommand request,
        CancellationToken cancellationToken)
    {
        var succeeded = new List<IngestDocumentResult>(request.Documents.Count);
        var errors = new List<string>();

        foreach (var doc in request.Documents)
        {
            try
            {
                var command = new IngestDocumentCommand(
                    doc.Title,
                    doc.Content,
                    request.OwnerId,
                    doc.Source,
                    doc.Metadata,
                    request.GenerateEmbeddings);

                var result = await _mediator.Send(command, cancellationToken);
                succeeded.Add(result);
            }
            catch (Exception ex)
            {
                var message = $"Failed to ingest '{doc.Title}': {ex.Message}";
                _logger.LogWarning(ex, "Bulk ingest: {Message}", message);
                errors.Add(message);
            }
        }

        return new BulkIngestDocumentsResult(
            TotalIngested: succeeded.Count,
            EmbeddingsGenerated: succeeded.Count(r => r.EmbeddingGenerated),
            Errors: errors);
    }
}
