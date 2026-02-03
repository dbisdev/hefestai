using Loremaster.Application.Features.Documents.DTOs;
using MediatR;

namespace Loremaster.Application.Features.Documents.Queries.SemanticSearch;

/// <summary>
/// Query for semantic search across documents using vector similarity.
/// </summary>
/// <param name="Query">The search query text.</param>
/// <param name="OwnerId">The owner ID to filter documents.</param>
/// <param name="Limit">Maximum number of results to return.</param>
/// <param name="Threshold">Minimum similarity threshold (0.0 to 1.0).</param>
/// <param name="ProjectId">Optional project ID to filter documents.</param>
/// <param name="GameSystemId">Optional game system ID to filter documents (for RAG on manuals).</param>
/// <param name="GenerateAnswer">Whether to generate a RAG answer from the results.</param>
/// <param name="SystemPrompt">Optional system prompt for RAG answer generation.</param>
public record SemanticSearchQuery(
    string Query,
    Guid OwnerId,
    int Limit = 5,
    float Threshold = 0.7f,
    Guid? ProjectId = null,
    Guid? GameSystemId = null,
    bool GenerateAnswer = false,
    string? SystemPrompt = null) : IRequest<SemanticSearchResult>;

public record SemanticSearchResult(
    IReadOnlyList<DocumentSearchResultDto> Documents,
    string? GeneratedAnswer = null);
