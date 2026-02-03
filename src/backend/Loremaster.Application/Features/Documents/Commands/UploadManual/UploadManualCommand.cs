using Loremaster.Domain.Enums;
using MediatR;

namespace Loremaster.Application.Features.Documents.Commands.UploadManual;

/// <summary>
/// Command to upload and process a PDF manual for a game system.
/// The PDF is parsed, chunked, and each chunk is embedded for RAG queries.
/// </summary>
/// <param name="GameSystemId">The game system this manual belongs to.</param>
/// <param name="OwnerId">The user uploading the manual.</param>
/// <param name="Title">Title for the manual (if not provided, extracted from PDF).</param>
/// <param name="PdfContent">The PDF file content as bytes.</param>
/// <param name="SourceType">Type of RAG source (Rulebook, Supplement, Custom).</param>
/// <param name="Version">Optional version identifier for the manual.</param>
public record UploadManualCommand(
    Guid GameSystemId,
    Guid OwnerId,
    string? Title,
    byte[] PdfContent,
    RagSourceType SourceType = RagSourceType.Rulebook,
    string? Version = null
) : IRequest<UploadManualResult>;

/// <summary>
/// Result of uploading a manual.
/// </summary>
/// <param name="ManualId">The ID of the parent document created.</param>
/// <param name="Title">The title of the manual.</param>
/// <param name="PageCount">Number of pages in the PDF.</param>
/// <param name="ChunkCount">Number of chunks created.</param>
/// <param name="EmbeddingsGenerated">Number of embeddings successfully generated.</param>
/// <param name="TotalCharacters">Total characters extracted from the PDF.</param>
public record UploadManualResult(
    Guid ManualId,
    string Title,
    int PageCount,
    int ChunkCount,
    int EmbeddingsGenerated,
    int TotalCharacters
);
