using MediatR;

namespace Loremaster.Application.Features.Documents.Commands.IngestDocument;

public record IngestDocumentCommand(
    string Title,
    string Content,
    Guid OwnerId,
    string? Source = null,
    string? Metadata = null,
    Guid? ProjectId = null,
    bool GenerateEmbedding = true) : IRequest<IngestDocumentResult>;

public record IngestDocumentResult(
    Guid DocumentId,
    bool EmbeddingGenerated,
    int? EmbeddingDimensions);
