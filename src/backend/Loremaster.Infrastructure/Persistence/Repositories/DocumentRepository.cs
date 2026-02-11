using Loremaster.Application.Common.Interfaces;
using Loremaster.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace Loremaster.Infrastructure.Persistence.Repositories;

/// <summary>
/// Document repository with pgvector semantic search support.
/// Handles document storage and retrieval including semantic search via pgvector.
/// </summary>
public class DocumentRepository : IDocumentRepository
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<DocumentRepository> _logger;
    private readonly NpgsqlDataSource? _dataSource;

    /// <summary>
    /// Initializes the DocumentRepository with required dependencies.
    /// </summary>
    /// <param name="context">The database context.</param>
    /// <param name="logger">Logger for diagnostics.</param>
    /// <param name="dataSource">Optional NpgsqlDataSource configured with UseVector() for pgvector support.
    /// When null, semantic search will return empty results (useful for testing with InMemory DB).</param>
    public DocumentRepository(
        ApplicationDbContext context, 
        ILogger<DocumentRepository> logger,
        NpgsqlDataSource? dataSource = null)
    {
        _context = context;
        _logger = logger;
        _dataSource = dataSource;
    }

    public async Task<Document?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Documents
            .Include(d => d.Owner)
            .FirstOrDefaultAsync(d => d.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<Document>> GetByOwnerIdAsync(Guid ownerId, CancellationToken cancellationToken = default)
    {
        return await _context.Documents
            .Where(d => d.OwnerId == ownerId)
            .OrderByDescending(d => d.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Semantic search using pgvector cosine distance operator (<=>)
    /// </summary>
    /// <param name="queryEmbedding">The embedding vector for the search query.</param>
    /// <param name="ownerId">The owner ID to filter documents.</param>
    /// <param name="limit">Maximum number of results to return.</param>
    /// <param name="threshold">Minimum similarity threshold (0.0 to 1.0).</param>
    /// <param name="gameSystemId">Optional game system ID to filter documents (for RAG on manuals).</param>
    /// <param name="skipOwnerFilter">When true, skips owner filtering (for admin operations).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of documents with similarity scores.</returns>
    public async Task<IReadOnlyList<DocumentSearchResult>> SemanticSearchAsync(
        float[] queryEmbedding,
        Guid ownerId,
        int limit = 5,
        float threshold = 0.7f,
        Guid? gameSystemId = null,
        bool skipOwnerFilter = false,
        CancellationToken cancellationToken = default)
    {
        // When NpgsqlDataSource is not available (e.g., in tests with InMemory DB),
        // semantic search is not supported - return empty results
        if (_dataSource == null)
        {
            _logger.LogWarning(
                "SemanticSearchAsync: NpgsqlDataSource not available. " +
                "Semantic search requires PostgreSQL with pgvector extension. Returning empty results.");
            return Array.Empty<DocumentSearchResult>();
        }
        
        // Convert embedding to pgvector format using invariant culture
        // IMPORTANT: Use InvariantCulture to ensure decimal point (.) is used, not comma
        // Spanish/European locales use comma as decimal separator which would break pgvector parsing
        var embeddingStr = $"[{string.Join(",", queryEmbedding.Select(v => v.ToString(System.Globalization.CultureInfo.InvariantCulture)))}]";
        
        _logger.LogInformation(
            "SemanticSearchAsync: Query embedding has {Dimensions} dimensions, embedding string length: {Length}",
            queryEmbedding.Length,
            embeddingStr.Length);
        
        // Build SQL with pgvector cosine distance
        // Note: cosine distance = 1 - cosine similarity, so we subtract from 1
        // IMPORTANT: We cast to vector(3072) explicitly to match the column definition (gemini-embedding-001)
        // Note: Table/column names use snake_case to match migration
        var sql = @"
            SELECT d.*, (1 - (d.embedding <=> @embedding::vector(3072))) as similarity
            FROM public.documents d
            WHERE d.embedding IS NOT NULL
            AND (1 - (d.embedding <=> @embedding::vector(3072))) >= @threshold";
        
        // Add owner filter unless skipped (for admin operations)
        if (!skipOwnerFilter)
        {
            sql += @" AND d.owner_id = @ownerId";
        }
        
        if (gameSystemId.HasValue)
        {
            sql += @" AND d.game_system_id = @gameSystemId";
        }
        
        sql += @"
            ORDER BY d.embedding <=> @embedding::vector(3072)
            LIMIT @limit";

        var parameters = new List<NpgsqlParameter>
        {
            new("embedding", embeddingStr),
            new("threshold", threshold),
            new("limit", limit)
        };

        // Only add ownerId parameter if we're filtering by owner
        if (!skipOwnerFilter)
        {
            parameters.Add(new NpgsqlParameter("ownerId", ownerId));
        }

        if (gameSystemId.HasValue)
        {
            parameters.Add(new NpgsqlParameter("gameSystemId", gameSystemId.Value));
        }

        try
        {
            // Use the configured NpgsqlDataSource which has UseVector() enabled
            // This ensures proper type handling for pgvector operations
            var results = new List<DocumentSearchResult>();
            
            await using var connection = await _dataSource.OpenConnectionAsync(cancellationToken);
            
            await using var command = new NpgsqlCommand(sql, connection);
            foreach (var param in parameters)
            {
                command.Parameters.Add(param);
            }

            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            
            while (await reader.ReadAsync(cancellationToken))
            {
                var document = await GetByIdAsync(reader.GetGuid(reader.GetOrdinal("id")), cancellationToken);
                if (document != null)
                {
                    var similarity = reader.GetFloat(reader.GetOrdinal("similarity"));
                    results.Add(new DocumentSearchResult(document, similarity));
                }
            }

            _logger.LogInformation(
                "Semantic search found {Count} documents{OwnerInfo}{GameSystemInfo}", 
                results.Count,
                skipOwnerFilter ? " (admin mode - all owners)" : $" for owner {ownerId}",
                gameSystemId.HasValue ? $" for game system {gameSystemId}" : "");

            return results;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Semantic search failed{OwnerInfo}", 
                skipOwnerFilter ? " (admin mode)" : $" for owner {ownerId}");
            throw;
        }
    }

    public async Task<Document> AddAsync(Document document, CancellationToken cancellationToken = default)
    {
        await _context.Documents.AddAsync(document, cancellationToken);
        return document;
    }

    public Task UpdateAsync(Document document, CancellationToken cancellationToken = default)
    {
        _context.Documents.Update(document);
        return Task.CompletedTask;
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var document = await GetByIdAsync(id, cancellationToken);
        if (document != null)
        {
            _context.Documents.Remove(document);
        }
    }

    public async Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Documents.AnyAsync(d => d.Id == id, cancellationToken);
    }

    /// <summary>
    /// Maximum content length allowed for embedding generation.
    /// Documents exceeding this limit will be skipped as they cannot be processed by the embedding model.
    /// </summary>
    private const int MaxEmbeddingContentLength = 8000;

    public async Task<IReadOnlyList<Document>> GetDocumentsWithoutEmbeddingAsync(
        Guid ownerId,
        int limit = 10,
        bool skipOwnerFilter = false,
        CancellationToken cancellationToken = default)
    {
        var query = _context.Documents
            .Where(d => d.Embedding == null 
                && d.Content.Length <= MaxEmbeddingContentLength);
        
        // Apply owner filter unless admin override
        if (!skipOwnerFilter)
        {
            query = query.Where(d => d.OwnerId == ownerId);
        }

        return await query
            .OrderBy(d => d.CreatedAt)
            .Take(limit)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Get a parent document (manual) with its chunk count.
    /// </summary>
    public async Task<ManualWithChunkCount?> GetManualWithChunkCountAsync(
        Guid manualId,
        Guid ownerId,
        CancellationToken cancellationToken = default)
    {
        var manual = await _context.Documents
            .Include(d => d.GameSystem)
            .FirstOrDefaultAsync(d => 
                d.Id == manualId && 
                d.OwnerId == ownerId && 
                d.ParentDocumentId == null, // Only parent documents (manuals)
                cancellationToken);

        if (manual == null)
            return null;

        var chunkCount = await _context.Documents
            .CountAsync(d => d.ParentDocumentId == manualId, cancellationToken);

        return new ManualWithChunkCount(manual, chunkCount);
    }

    /// <summary>
    /// Get all parent documents (manuals) for a game system with chunk counts.
    /// </summary>
    public async Task<IReadOnlyList<ManualWithChunkCount>> GetManualsByGameSystemIdAsync(
        Guid gameSystemId,
        Guid ownerId,
        CancellationToken cancellationToken = default)
    {
        // Get all parent documents (manuals) for the game system
        var manuals = await _context.Documents
            .Include(d => d.GameSystem)
            .Where(d => 
                d.GameSystemId == gameSystemId && 
                d.OwnerId == ownerId && 
                d.ParentDocumentId == null) // Only parent documents
            .OrderByDescending(d => d.CreatedAt)
            .ToListAsync(cancellationToken);

        // Get chunk counts for each manual
        var manualIds = manuals.Select(m => m.Id).ToList();
        var chunkCounts = await _context.Documents
            .Where(d => d.ParentDocumentId.HasValue && manualIds.Contains(d.ParentDocumentId.Value))
            .GroupBy(d => d.ParentDocumentId!.Value)
            .Select(g => new { ManualId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.ManualId, x => x.Count, cancellationToken);

        return manuals
            .Select(m => new ManualWithChunkCount(m, chunkCounts.GetValueOrDefault(m.Id, 0)))
            .ToList();
    }
}
