using Loremaster.Application.Features.Documents.Commands.GenerateMissingEmbeddings;
using Loremaster.Application.Features.Documents.Commands.IngestDocument;
using Loremaster.Application.Features.Documents.Commands.UploadManual;
using Loremaster.Application.Features.Documents.DTOs;
using Loremaster.Application.Features.Documents.Queries.CheckDocumentAvailability;
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
[Authorize] // Base authorization - must be authenticated
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
    [Authorize(Policy = "RequireMasterRole")]
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
    /// Semantic search across documents.
    /// Players can search Admin-shared documents and optionally their campaign Master's documents.
    /// Masters can search Admin-shared documents + their own documents.
    /// </summary>
    /// <param name="request">Search request with query and optional filters.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Search results with optional generated answer.</returns>
    [HttpPost("search")]
    [Authorize(Policy = "RequirePlayerRole")]
    [ProducesResponseType(typeof(SemanticSearchResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<SemanticSearchResult>> SemanticSearch(
        [FromBody] SemanticSearchRequest request,
        CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        var userRole = GetCurrentUserRole();
        
        // Determine the owner ID to use for filtering:
        // - Masters: use their own ID (see Admin + own docs)
        // - Players: use MasterId if provided (see Admin + campaign Master's docs), otherwise just Admin docs
        var ownerId = userRole == UserRole.Player 
            ? request.MasterId ?? userId  // Player: use masterId if provided, fallback to userId (only Admin docs will match)
            : userId;                      // Master: always use their own ID
        
        var query = new SemanticSearchQuery(
            request.Query,
            ownerId,
            request.Limit ?? 5,
            request.Threshold ?? 0.7f,
            request.GameSystemId,
            request.GenerateAnswer ?? false,
            request.SystemPrompt,
            IncludeAdminDocs: true); // Always include Admin-shared documents for RAG

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
    [Authorize(Policy = "RequireMasterRole")]
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
    [Authorize(Policy = "RequireMasterRole")]
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
    [Authorize(Policy = "RequireMasterRole")]
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
    /// Check if a game system has documents available for RAG search.
    /// Accessible by Players - checks for Admin-shared documents and optionally Master's documents.
    /// Masters also see their own documents in addition to Admin docs.
    /// </summary>
    /// <param name="gameSystemId">The game system ID to check.</param>
    /// <param name="masterId">Optional Master ID to include their documents (for Players in a campaign).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Document availability status.</returns>
    [HttpGet("game-systems/{gameSystemId:guid}/available")]
    [Authorize(Policy = "RequirePlayerRole")]
    [ProducesResponseType(typeof(DocumentAvailabilityDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<DocumentAvailabilityDto>> CheckDocumentAvailability(
        [FromRoute] Guid gameSystemId,
        [FromQuery] Guid? masterId,
        CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        var userRole = GetCurrentUserRole();
        
        // Determine the owner ID to use for filtering:
        // - Admins: see all documents (IncludeAllDocs = true)
        // - Masters: use their own ID (see Admin + own docs)
        // - Players: use masterId if provided (see Admin + campaign Master's docs), otherwise null (Admin only)
        Guid? ownerId = userRole == UserRole.Player ? masterId : userId;
        bool includeAllDocs = userRole == UserRole.Admin;

        var query = new CheckDocumentAvailabilityQuery(
            gameSystemId,
            ownerId,
            IncludeAdminDocs: true,
            IncludeAllDocs: includeAllDocs);

        var result = await _mediator.Send(query, cancellationToken);

        return Ok(new DocumentAvailabilityDto(result.HasDocuments, result.GameSystemId));
    }

    /// <summary>
    /// Bulk ingest multiple documents
    /// </summary>
    [HttpPost("ingest/bulk")]
    [Authorize(Policy = "RequireMasterRole")]
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
    [Authorize(Policy = "RequireMasterRole")]
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

    /// <summary>
    /// Gets the current user's role from JWT claims.
    /// </summary>
    private UserRole GetCurrentUserRole()
    {
        var roleClaim = User.FindFirst("role")?.Value
            ?? User.FindFirst(ClaimTypes.Role)?.Value;
        
        return roleClaim switch
        {
            "Admin" => UserRole.Admin,
            "Master" => UserRole.Master,
            _ => UserRole.Player
        };
    }
}

// Request DTOs
public record IngestDocumentRequest(
    string Title,
    string Content,
    string? Source = null,
    string? Metadata = null,
    bool? GenerateEmbedding = true);

/// <summary>
/// Request for semantic search across documents.
/// </summary>
/// <param name="Query">The search query text.</param>
/// <param name="Limit">Maximum number of results (default 5).</param>
/// <param name="Threshold">Minimum similarity threshold (default 0.7).</param>
/// <param name="GameSystemId">Optional game system ID to filter documents.</param>
/// <param name="GenerateAnswer">Whether to generate a RAG answer from results.</param>
/// <param name="SystemPrompt">Optional system prompt for RAG answer generation.</param>
/// <param name="MasterId">Optional Master ID for Players to access campaign Master's documents.</param>
public record SemanticSearchRequest(
    string Query,
    int? Limit = 5,
    float? Threshold = 0.7f,
    Guid? GameSystemId = null,
    bool? GenerateAnswer = false,
    string? SystemPrompt = null,
    Guid? MasterId = null);

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

/// <summary>
/// DTO for document availability check response.
/// </summary>
/// <param name="HasDocuments">Whether documents are available for RAG search.</param>
/// <param name="GameSystemId">The game system ID that was checked.</param>
public record DocumentAvailabilityDto(
    bool HasDocuments,
    Guid GameSystemId);
