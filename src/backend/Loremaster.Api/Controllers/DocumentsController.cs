using Loremaster.Application.Features.Documents.Commands.IngestDocument;
using Loremaster.Application.Features.Documents.DTOs;
using Loremaster.Application.Features.Documents.Queries.SemanticSearch;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using System.Security.Claims;

namespace Loremaster.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Policy = "RequireMasterRole")]
[EnableRateLimiting("api")]
public class DocumentsController : ControllerBase
{
    private readonly IMediator _mediator;

    public DocumentsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Ingest a document for RAG (generates embedding automatically)
    /// </summary>
    [HttpPost("ingest")]
    [ProducesResponseType(typeof(IngestDocumentResult), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<IngestDocumentResult>> IngestDocument(
        [FromBody] IngestDocumentRequest request,
        CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        
        var command = new IngestDocumentCommand(
            request.Title,
            request.Content,
            userId,
            request.Source,
            request.Metadata,
            request.ProjectId,
            request.GenerateEmbedding ?? true);

        var result = await _mediator.Send(command, cancellationToken);
        
        return CreatedAtAction(nameof(IngestDocument), new { id = result.DocumentId }, result);
    }

    /// <summary>
    /// Semantic search across documents
    /// </summary>
    [HttpPost("search")]
    [ProducesResponseType(typeof(SemanticSearchResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<SemanticSearchResult>> SemanticSearch(
        [FromBody] SemanticSearchRequest request,
        CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();

        var query = new SemanticSearchQuery(
            request.Query,
            userId,
            request.Limit ?? 5,
            request.Threshold ?? 0.7f,
            request.ProjectId,
            request.GenerateAnswer ?? false,
            request.SystemPrompt);

        var result = await _mediator.Send(query, cancellationToken);
        
        return Ok(result);
    }

    /// <summary>
    /// Bulk ingest multiple documents
    /// </summary>
    [HttpPost("ingest/bulk")]
    [ProducesResponseType(typeof(BulkIngestResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<BulkIngestResult>> BulkIngestDocuments(
        [FromBody] BulkIngestRequest request,
        CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        var results = new List<IngestDocumentResult>();
        var errors = new List<string>();

        foreach (var doc in request.Documents)
        {
            try
            {
                var command = new IngestDocumentCommand(
                    doc.Title,
                    doc.Content,
                    userId,
                    doc.Source,
                    doc.Metadata,
                    request.ProjectId,
                    request.GenerateEmbeddings ?? true);

                var result = await _mediator.Send(command, cancellationToken);
                results.Add(result);
            }
            catch (Exception ex)
            {
                errors.Add($"Failed to ingest '{doc.Title}': {ex.Message}");
            }
        }

        return Ok(new BulkIngestResult(
            results.Count,
            results.Count(r => r.EmbeddingGenerated),
            errors));
    }

    private Guid GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? User.FindFirst("sub")?.Value;
        
        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
        {
            throw new UnauthorizedAccessException("Invalid user token");
        }

        return userId;
    }
}

// Request DTOs
public record IngestDocumentRequest(
    string Title,
    string Content,
    string? Source = null,
    string? Metadata = null,
    Guid? ProjectId = null,
    bool? GenerateEmbedding = true);

public record SemanticSearchRequest(
    string Query,
    int? Limit = 5,
    float? Threshold = 0.7f,
    Guid? ProjectId = null,
    bool? GenerateAnswer = false,
    string? SystemPrompt = null);

public record BulkIngestRequest(
    List<BulkDocumentItem> Documents,
    Guid? ProjectId = null,
    bool? GenerateEmbeddings = true);

public record BulkDocumentItem(
    string Title,
    string Content,
    string? Source = null,
    string? Metadata = null);

public record BulkIngestResult(
    int TotalIngested,
    int EmbeddingsGenerated,
    List<string> Errors);
