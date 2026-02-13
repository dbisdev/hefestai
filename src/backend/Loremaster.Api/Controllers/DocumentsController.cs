using Loremaster.Application.Features.Documents.Commands.GenerateMissingEmbeddings;
using Loremaster.Application.Features.Documents.Commands.IngestDocument;
using Loremaster.Application.Features.Documents.Commands.UploadManual;
using Loremaster.Application.Features.Documents.DTOs;
using Loremaster.Application.Features.Documents.Queries.GetManual;
using Loremaster.Application.Features.Documents.Queries.GetManualsByGameSystem;
using Loremaster.Application.Features.Documents.Queries.SemanticSearch;
using Loremaster.Domain.Enums;
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
            request.GameSystemId,
            request.GenerateAnswer ?? false,
            request.SystemPrompt);

        var result = await _mediator.Send(query, cancellationToken);
        
        return Ok(result);
    }

    /// <summary>
    /// Upload a PDF manual for a game system (RAG ingestion with chunking)
    /// </summary>
    /// <param name="gameSystemId">The game system this manual belongs to.</param>
    /// <param name="request">The upload request containing PDF file and metadata.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Result containing manual ID and chunk statistics.</returns>
    [HttpPost("game-systems/{gameSystemId:guid}/manuals")]
    [ProducesResponseType(typeof(UploadManualResult), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [RequestSizeLimit(52_428_800)] // 50 MB limit
    public async Task<ActionResult<UploadManualResult>> UploadManual(
        [FromRoute] Guid gameSystemId,
        [FromForm] UploadManualRequest request,
        CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();

        if (request.File == null || request.File.Length == 0)
        {
            return BadRequest("PDF file is required");
        }

        // Read file content into byte array
        byte[] pdfContent;
        using (var memoryStream = new MemoryStream())
        {
            await request.File.CopyToAsync(memoryStream, cancellationToken);
            pdfContent = memoryStream.ToArray();
        }

        var command = new UploadManualCommand(
            gameSystemId,
            userId,
            request.Title,
            pdfContent,
            request.SourceType ?? RagSourceType.Rulebook,
            request.Version);

        var result = await _mediator.Send(command, cancellationToken);

        return CreatedAtAction(
            nameof(GetManual),
            new { gameSystemId, manualId = result.ManualId },
            result);
    }

    /// <summary>
    /// Get a manual by ID
    /// </summary>
    /// <param name="gameSystemId">The game system ID.</param>
    /// <param name="manualId">The manual document ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The manual document details.</returns>
    [HttpGet("game-systems/{gameSystemId:guid}/manuals/{manualId:guid}")]
    [ProducesResponseType(typeof(ManualDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ManualDto>> GetManual(
        [FromRoute] Guid gameSystemId,
        [FromRoute] Guid manualId,
        CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();

        var query = new GetManualQuery(manualId, gameSystemId, userId);
        var result = await _mediator.Send(query, cancellationToken);

        if (result == null)
        {
            return NotFound($"Manual with ID {manualId} not found");
        }

        return Ok(new ManualDto(
            result.Id,
            result.GameSystemId,
            result.Title,
            PageCount: 0, // PageCount is not stored, could be calculated from chunks
            result.ChunkCount,
            result.SourceType ?? RagSourceType.Rulebook,
            result.Source,
            result.CreatedAt));
    }

    /// <summary>
    /// Get all manuals for a game system with chunk counts.
    /// Used to check if a game system has documents available for RAG search.
    /// </summary>
    /// <param name="gameSystemId">The game system ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of manuals with chunk counts.</returns>
    [HttpGet("game-systems/{gameSystemId:guid}/manuals")]
    [ProducesResponseType(typeof(IReadOnlyList<ManualSummaryDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<IReadOnlyList<ManualSummaryDto>>> GetManualsByGameSystem(
        [FromRoute] Guid gameSystemId,
        CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();

        var query = new GetManualsByGameSystemQuery(gameSystemId, userId);
        var results = await _mediator.Send(query, cancellationToken);

        return Ok(results.Select(r => new ManualSummaryDto(
            r.Id,
            r.GameSystemId,
            r.Title,
            r.ChunkCount,
            r.SourceType ?? RagSourceType.Rulebook,
            r.Version,
            r.CreatedAt)));
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

    /// <summary>
    /// Generate embeddings for documents that don't have them.
    /// Useful for backfilling embeddings after manual imports or migration.
    /// </summary>
    /// <param name="request">Request parameters for batch processing.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Result with success/failure counts.</returns>
    [HttpPost("embeddings/generate")]
    [ProducesResponseType(typeof(GenerateMissingEmbeddingsResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<GenerateMissingEmbeddingsResult>> GenerateMissingEmbeddings(
        [FromBody] GenerateMissingEmbeddingsRequest request,
        CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        var isAdmin = IsCurrentUserAdmin();

        var command = new GenerateMissingEmbeddingsCommand(
            userId,
            request.BatchSize ?? 10,
            request.MaxDocuments ?? 100,
            request.GameSystemId,
            isAdmin);

        var result = await _mediator.Send(command, cancellationToken);

        return Ok(result);
    }

    private Guid GetCurrentUserId()
    {
        // Try JWT standard claim "sub" first (when MapInboundClaims = false)
        // Fall back to ClaimTypes.NameIdentifier for compatibility
        var userIdClaim = User.FindFirst("sub")?.Value
            ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        
        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
        {
            throw new UnauthorizedAccessException("Invalid user token");
        }

        return userId;
    }

    /// <summary>
    /// Checks if the current user has the Admin role.
    /// </summary>
    private bool IsCurrentUserAdmin()
    {
        var roleClaim = User.FindFirst("role")?.Value
            ?? User.FindFirst(ClaimTypes.Role)?.Value;
        
        return roleClaim == "Admin";
    }
}

// Request DTOs
public record IngestDocumentRequest(
    string Title,
    string Content,
    string? Source = null,
    string? Metadata = null,
    bool? GenerateEmbedding = true);

public record SemanticSearchRequest(
    string Query,
    int? Limit = 5,
    float? Threshold = 0.7f,
    Guid? GameSystemId = null,
    bool? GenerateAnswer = false,
    string? SystemPrompt = null);

public record BulkIngestRequest(
    List<BulkDocumentItem> Documents,
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

/// <summary>
/// Request for uploading a PDF manual.
/// </summary>
/// <param name="File">The PDF file to upload.</param>
/// <param name="Title">Optional title (extracted from PDF if not provided).</param>
/// <param name="SourceType">Type of RAG source (Rulebook, Supplement, Custom).</param>
/// <param name="Version">Optional version identifier.</param>
public record UploadManualRequest(
    IFormFile File,
    string? Title = null,
    RagSourceType? SourceType = null,
    string? Version = null);

/// <summary>
/// DTO for manual document information.
/// </summary>
public record ManualDto(
    Guid Id,
    Guid GameSystemId,
    string Title,
    int PageCount,
    int ChunkCount,
    RagSourceType SourceType,
    string? Version,
    DateTime CreatedAt);

/// <summary>
/// Summary DTO for manual listing (without page count).
/// </summary>
public record ManualSummaryDto(
    Guid Id,
    Guid GameSystemId,
    string Title,
    int ChunkCount,
    RagSourceType SourceType,
    string? Version,
    DateTime CreatedAt);

/// <summary>
/// Request for generating missing embeddings.
/// </summary>
/// <param name="BatchSize">Number of documents to process per batch (default 10).</param>
/// <param name="MaxDocuments">Maximum total documents to process (default 100, 0 = unlimited).</param>
/// <param name="GameSystemId">Optional filter by game system.</param>
public record GenerateMissingEmbeddingsRequest(
    int? BatchSize = 10,
    int? MaxDocuments = 100,
    Guid? GameSystemId = null);
