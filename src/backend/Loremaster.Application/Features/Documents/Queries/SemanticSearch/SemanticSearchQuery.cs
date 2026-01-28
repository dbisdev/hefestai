using Loremaster.Application.Features.Documents.DTOs;
using MediatR;

namespace Loremaster.Application.Features.Documents.Queries.SemanticSearch;

public record SemanticSearchQuery(
    string Query,
    Guid OwnerId,
    int Limit = 5,
    float Threshold = 0.7f,
    Guid? ProjectId = null,
    bool GenerateAnswer = false,
    string? SystemPrompt = null) : IRequest<SemanticSearchResult>;

public record SemanticSearchResult(
    IReadOnlyList<DocumentSearchResultDto> Documents,
    string? GeneratedAnswer = null);
