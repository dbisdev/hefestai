namespace Loremaster.Application.Features.Documents.DTOs;

public record DocumentDto(
    Guid Id,
    string Title,
    string Content,
    string? Source,
    string? Metadata,
    bool HasEmbedding,
    int? EmbeddingDimensions,
    Guid OwnerId,
    Guid? ProjectId,
    DateTime CreatedAt,
    DateTime? UpdatedAt);

public record DocumentSearchResultDto(
    DocumentDto Document,
    float SimilarityScore);
