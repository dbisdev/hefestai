using MediatR;

namespace Loremaster.Application.Features.Documents.Commands.BulkIngestDocuments;

/// <summary>
/// Represents a single document item within a bulk ingest request.
/// </summary>
/// <param name="Title">Document title.</param>
/// <param name="Content">Document body text.</param>
/// <param name="Source">Optional source identifier (URL, file path, etc.).</param>
/// <param name="Metadata">Optional JSON metadata string.</param>
public record BulkDocumentItem(
    string Title,
    string Content,
    string? Source = null,
    string? Metadata = null);

/// <summary>
/// Command to ingest multiple documents in a single operation.
/// Each document is processed individually; failures are captured rather than aborting the batch.
/// </summary>
/// <param name="Documents">List of documents to ingest.</param>
/// <param name="OwnerId">ID of the user performing the ingest (used as document owner).</param>
/// <param name="GenerateEmbeddings">Whether to generate embeddings for each document.</param>
public record BulkIngestDocumentsCommand(
    IReadOnlyList<BulkDocumentItem> Documents,
    Guid OwnerId,
    bool GenerateEmbeddings = true) : IRequest<BulkIngestDocumentsResult>;

/// <summary>
/// Result of a bulk document ingest operation.
/// </summary>
/// <param name="TotalIngested">Number of documents successfully ingested.</param>
/// <param name="EmbeddingsGenerated">Number of documents that received an embedding.</param>
/// <param name="Errors">Error messages for documents that failed to ingest.</param>
public record BulkIngestDocumentsResult(
    int TotalIngested,
    int EmbeddingsGenerated,
    IReadOnlyList<string> Errors);
