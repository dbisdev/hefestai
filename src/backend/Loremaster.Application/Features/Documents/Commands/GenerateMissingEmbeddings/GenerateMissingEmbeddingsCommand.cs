using MediatR;

namespace Loremaster.Application.Features.Documents.Commands.GenerateMissingEmbeddings;

/// <summary>
/// Command to generate embeddings for documents that don't have them.
/// Processes documents in batches to avoid memory and API rate limit issues.
/// </summary>
/// <param name="OwnerId">The owner whose documents should be processed.</param>
/// <param name="BatchSize">Number of documents to process per batch (default 10).</param>
/// <param name="MaxDocuments">Maximum total documents to process (default 100, 0 = unlimited).</param>
/// <param name="GameSystemId">Optional filter by game system.</param>
public record GenerateMissingEmbeddingsCommand(
    Guid OwnerId,
    int BatchSize = 10,
    int MaxDocuments = 100,
    Guid? GameSystemId = null) : IRequest<GenerateMissingEmbeddingsResult>;

/// <summary>
/// Result of the embedding generation process.
/// </summary>
/// <param name="TotalProcessed">Total number of documents processed.</param>
/// <param name="SuccessCount">Number of documents successfully embedded.</param>
/// <param name="FailureCount">Number of documents that failed to embed.</param>
/// <param name="Errors">List of error messages for failed documents.</param>
public record GenerateMissingEmbeddingsResult(
    int TotalProcessed,
    int SuccessCount,
    int FailureCount,
    List<string> Errors);
