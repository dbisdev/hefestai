using Loremaster.Domain.Enums;
using MediatR;

namespace Loremaster.Application.Features.Documents.Queries.GetManualsByGameSystem;

/// <summary>
/// Query to retrieve all manuals for a game system with their chunk counts.
/// Used to check if a game system has documents available for RAG search.
/// </summary>
/// <param name="GameSystemId">The game system ID to filter manuals.</param>
/// <param name="OwnerId">The owner ID for authorization.</param>
public record GetManualsByGameSystemQuery(
    Guid GameSystemId,
    Guid OwnerId) : IRequest<IReadOnlyList<ManualSummaryResult>>;

/// <summary>
/// Summary result for a manual including basic info and chunk count.
/// </summary>
public record ManualSummaryResult(
    Guid Id,
    Guid GameSystemId,
    string Title,
    int ChunkCount,
    RagSourceType? SourceType,
    string? Version,
    DateTime CreatedAt);
