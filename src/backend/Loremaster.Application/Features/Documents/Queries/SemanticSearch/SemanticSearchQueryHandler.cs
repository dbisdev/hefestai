using Loremaster.Application.Common.Interfaces;
using Loremaster.Application.Features.Documents.DTOs;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Loremaster.Application.Features.Documents.Queries.SemanticSearch;

public class SemanticSearchQueryHandler : IRequestHandler<SemanticSearchQuery, SemanticSearchResult>
{
    private readonly IDocumentRepository _documentRepository;
    private readonly IEmbeddingService _embeddingService;
    private readonly ILogger<SemanticSearchQueryHandler> _logger;

    public SemanticSearchQueryHandler(
        IDocumentRepository documentRepository,
        IEmbeddingService embeddingService,
        ILogger<SemanticSearchQueryHandler> logger)
    {
        _documentRepository = documentRepository;
        _embeddingService = embeddingService;
        _logger = logger;
    }

    public async Task<SemanticSearchResult> Handle(SemanticSearchQuery request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Semantic search for query: {Query} by owner {OwnerId}", 
            request.Query.Substring(0, Math.Min(50, request.Query.Length)), request.OwnerId);

        // Generate embedding for the query
        var queryEmbedding = await _embeddingService.GetEmbeddingAsync(request.Query, cancellationToken);

        // Perform semantic search
        var searchResults = await _documentRepository.SemanticSearchAsync(
            queryEmbedding,
            request.OwnerId,
            request.Limit,
            request.Threshold,
            request.ProjectId,
            cancellationToken);

        // Map to DTOs
        var documentResults = searchResults.Select(r => new DocumentSearchResultDto(
            new DocumentDto(
                r.Document.Id,
                r.Document.Title,
                r.Document.Content,
                r.Document.Source,
                r.Document.Metadata,
                r.Document.HasEmbedding,
                r.Document.EmbeddingDimensions,
                r.Document.OwnerId,
                r.Document.ProjectId,
                r.Document.CreatedAt,
                r.Document.UpdatedAt),
            r.SimilarityScore
        )).ToList();

        _logger.LogInformation("Found {Count} documents matching query", documentResults.Count);

        string? generatedAnswer = null;

        // Generate RAG answer if requested and we have results
        if (request.GenerateAnswer && documentResults.Count > 0)
        {
            try
            {
                var context = documentResults.Select(d => d.Document.Content).ToList();
                var ragResult = await _embeddingService.GenerateWithContextAsync(
                    request.Query,
                    context,
                    request.SystemPrompt,
                    cancellationToken: cancellationToken);

                generatedAnswer = ragResult.Answer;
                _logger.LogDebug("Generated RAG answer for query");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to generate RAG answer");
            }
        }

        return new SemanticSearchResult(documentResults, generatedAnswer);
    }
}
