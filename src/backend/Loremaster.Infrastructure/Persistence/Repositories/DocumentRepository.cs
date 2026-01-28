using Loremaster.Application.Common.Interfaces;
using Loremaster.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace Loremaster.Infrastructure.Persistence.Repositories;

/// <summary>
/// Document repository with pgvector semantic search support
/// </summary>
public class DocumentRepository : IDocumentRepository
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<DocumentRepository> _logger;

    public DocumentRepository(ApplicationDbContext context, ILogger<DocumentRepository> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<Document?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Documents
            .Include(d => d.Owner)
            .Include(d => d.Project)
            .FirstOrDefaultAsync(d => d.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<Document>> GetByOwnerIdAsync(Guid ownerId, CancellationToken cancellationToken = default)
    {
        return await _context.Documents
            .Where(d => d.OwnerId == ownerId)
            .OrderByDescending(d => d.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Document>> GetByProjectIdAsync(Guid projectId, CancellationToken cancellationToken = default)
    {
        return await _context.Documents
            .Where(d => d.ProjectId == projectId)
            .OrderByDescending(d => d.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Semantic search using pgvector cosine distance operator (<=>)
    /// </summary>
    public async Task<IReadOnlyList<DocumentSearchResult>> SemanticSearchAsync(
        float[] queryEmbedding,
        Guid ownerId,
        int limit = 5,
        float threshold = 0.7f,
        Guid? projectId = null,
        CancellationToken cancellationToken = default)
    {
        // Convert embedding to pgvector format
        var embeddingStr = $"[{string.Join(",", queryEmbedding)}]";
        
        // Build SQL with pgvector cosine distance
        // Note: cosine distance = 1 - cosine similarity, so we subtract from 1
        var sql = @"
            SELECT d.*, (1 - (d.""Embedding"" <=> @embedding::vector)) as similarity
            FROM ""Documents"" d
            WHERE d.""OwnerId"" = @ownerId
            AND d.""Embedding"" IS NOT NULL
            AND (1 - (d.""Embedding"" <=> @embedding::vector)) >= @threshold";
        
        if (projectId.HasValue)
        {
            sql += @" AND d.""ProjectId"" = @projectId";
        }
        
        sql += @"
            ORDER BY d.""Embedding"" <=> @embedding::vector
            LIMIT @limit";

        var parameters = new List<NpgsqlParameter>
        {
            new("embedding", embeddingStr),
            new("ownerId", ownerId),
            new("threshold", threshold),
            new("limit", limit)
        };

        if (projectId.HasValue)
        {
            parameters.Add(new NpgsqlParameter("projectId", projectId.Value));
        }

        try
        {
            // Use raw SQL for pgvector operations
            var results = new List<DocumentSearchResult>();
            
            await using var connection = new NpgsqlConnection(_context.Database.GetConnectionString());
            await connection.OpenAsync(cancellationToken);
            
            await using var command = new NpgsqlCommand(sql, connection);
            foreach (var param in parameters)
            {
                command.Parameters.Add(param);
            }

            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            
            while (await reader.ReadAsync(cancellationToken))
            {
                var document = await GetByIdAsync(reader.GetGuid(reader.GetOrdinal("Id")), cancellationToken);
                if (document != null)
                {
                    var similarity = reader.GetFloat(reader.GetOrdinal("similarity"));
                    results.Add(new DocumentSearchResult(document, similarity));
                }
            }

            _logger.LogInformation(
                "Semantic search found {Count} documents for owner {OwnerId}", 
                results.Count, ownerId);

            return results;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Semantic search failed for owner {OwnerId}", ownerId);
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

    public async Task<IReadOnlyList<Document>> GetDocumentsWithoutEmbeddingAsync(
        Guid ownerId,
        int limit = 10,
        CancellationToken cancellationToken = default)
    {
        return await _context.Documents
            .Where(d => d.OwnerId == ownerId && d.Embedding == null)
            .OrderBy(d => d.CreatedAt)
            .Take(limit)
            .ToListAsync(cancellationToken);
    }
}
