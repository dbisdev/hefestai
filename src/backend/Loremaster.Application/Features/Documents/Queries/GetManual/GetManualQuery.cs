using Loremaster.Domain.Enums;
using MediatR;

namespace Loremaster.Application.Features.Documents.Queries.GetManual;

/// <summary>
/// Query to retrieve a manual (parent document) by ID with its chunk count.
/// </summary>
/// <param name="ManualId">The manual document ID.</param>
/// <param name="GameSystemId">The game system ID (for route validation).</param>
/// <param name="OwnerId">The owner ID for authorization.</param>
public record GetManualQuery(
    Guid ManualId,
    Guid GameSystemId,
    Guid OwnerId) : IRequest<GetManualResult?>;

/// <summary>
/// Result containing manual details with chunk information.
/// </summary>
public record GetManualResult(
    Guid Id,
    Guid GameSystemId,
    string Title,
    int ChunkCount,
    RagSourceType? SourceType,
    string? Source,
    DateTime CreatedAt,
    DateTime? UpdatedAt);
