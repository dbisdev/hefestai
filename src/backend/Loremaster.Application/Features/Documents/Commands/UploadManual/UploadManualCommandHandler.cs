using Loremaster.Application.Common.Interfaces;
using Loremaster.Domain.Entities;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Loremaster.Application.Features.Documents.Commands.UploadManual;

/// <summary>
/// Handler for UploadManualCommand.
/// Parses PDF, chunks content, creates documents, and generates embeddings.
/// </summary>
public class UploadManualCommandHandler : IRequestHandler<UploadManualCommand, UploadManualResult>
{
    private readonly IPdfParsingService _pdfParsingService;
    private readonly ITextChunkingService _textChunkingService;
    private readonly IDocumentRepository _documentRepository;
    private readonly IGameSystemRepository _gameSystemRepository;
    private readonly IEmbeddingService _embeddingService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<UploadManualCommandHandler> _logger;

    public UploadManualCommandHandler(
        IPdfParsingService pdfParsingService,
        ITextChunkingService textChunkingService,
        IDocumentRepository documentRepository,
        IGameSystemRepository gameSystemRepository,
        IEmbeddingService embeddingService,
        IUnitOfWork unitOfWork,
        ILogger<UploadManualCommandHandler> logger)
    {
        _pdfParsingService = pdfParsingService;
        _textChunkingService = textChunkingService;
        _documentRepository = documentRepository;
        _gameSystemRepository = gameSystemRepository;
        _embeddingService = embeddingService;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<UploadManualResult> Handle(UploadManualCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Processing manual upload for GameSystem {GameSystemId} by user {OwnerId}",
            request.GameSystemId, request.OwnerId);

        // Validate game system exists
        var gameSystem = await _gameSystemRepository.GetByIdAsync(request.GameSystemId, cancellationToken);
        if (gameSystem == null)
        {
            throw new ArgumentException($"Game system with ID {request.GameSystemId} not found");
        }

        // Parse PDF
        _logger.LogDebug("Parsing PDF content ({ByteCount} bytes)", request.PdfContent.Length);
        var parseResult = await _pdfParsingService.ParseAsync(request.PdfContent, cancellationToken);
        
        if (string.IsNullOrWhiteSpace(parseResult.Content))
        {
            throw new InvalidOperationException("PDF parsing resulted in empty content");
        }

        // Determine title
        var title = !string.IsNullOrWhiteSpace(request.Title) 
            ? request.Title 
            : parseResult.Title ?? $"{gameSystem.Name} - {request.SourceType}";

        _logger.LogInformation(
            "PDF parsed: {PageCount} pages, {CharCount} characters, Title: {Title}",
            parseResult.PageCount, parseResult.Content.Length, title);

        // Create parent document (stores full content)
        var parentDocument = Document.Create(
            title: title,
            content: parseResult.Content,
            ownerId: request.OwnerId,
            source: $"PDF Upload - {request.Version ?? "v1.0"}",
            metadata: CreateMetadata(parseResult, request),
            gameSystemId: request.GameSystemId,
            sourceType: request.SourceType);

        await _documentRepository.AddAsync(parentDocument, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogDebug("Parent document created with ID {DocumentId}", parentDocument.Id);

        // Chunk the content
        var chunks = _textChunkingService.ChunkText(parseResult.Content, ChunkingOptions.Default);
        _logger.LogInformation("Content split into {ChunkCount} chunks", chunks.Count);

        // Create chunk documents and generate embeddings
        var embeddingsGenerated = 0;
        var chunkDocuments = new List<Document>();

        foreach (var chunk in chunks)
        {
            var chunkTitle = $"{title} - Chunk {chunk.Index + 1}/{chunks.Count}";
            var chunkDocument = Document.CreateChunk(
                title: chunkTitle,
                content: chunk.Content,
                ownerId: request.OwnerId,
                parentDocumentId: parentDocument.Id,
                chunkIndex: chunk.Index,
                source: $"Chunk {chunk.Index + 1} (chars {chunk.StartOffset}-{chunk.EndOffset})",
                gameSystemId: request.GameSystemId,
                sourceType: request.SourceType);

            chunkDocuments.Add(chunkDocument);
        }

        // Batch add all chunk documents
        foreach (var chunkDoc in chunkDocuments)
        {
            await _documentRepository.AddAsync(chunkDoc, cancellationToken);
        }
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Generate embeddings in batches
        const int batchSize = 10;
        for (var i = 0; i < chunkDocuments.Count; i += batchSize)
        {
            var batch = chunkDocuments.Skip(i).Take(batchSize).ToList();
            var contents = batch.Select(d => d.Content).ToArray();

            try
            {
                _logger.LogDebug("Generating embeddings for batch {BatchNum}/{TotalBatches}", 
                    (i / batchSize) + 1, (chunkDocuments.Count + batchSize - 1) / batchSize);

                var embeddingsResult = await _embeddingService.GetEmbeddingsAsync(contents, cancellationToken);

                for (var j = 0; j < batch.Count && j < embeddingsResult.Embeddings.Length; j++)
                {
                    batch[j].SetEmbedding(embeddingsResult.Embeddings[j]);
                    embeddingsGenerated++;
                }

                await _unitOfWork.SaveChangesAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, 
                    "Failed to generate embeddings for batch starting at index {StartIndex}. Continuing with remaining batches.",
                    i);
            }
        }

        _logger.LogInformation(
            "Manual upload complete: {Title}, {PageCount} pages, {ChunkCount} chunks, {EmbeddingsGenerated} embeddings",
            title, parseResult.PageCount, chunkDocuments.Count, embeddingsGenerated);

        return new UploadManualResult(
            ManualId: parentDocument.Id,
            Title: title,
            PageCount: parseResult.PageCount,
            ChunkCount: chunkDocuments.Count,
            EmbeddingsGenerated: embeddingsGenerated,
            TotalCharacters: parseResult.Content.Length);
    }

    /// <summary>
    /// Creates JSON metadata for the document.
    /// </summary>
    private static string CreateMetadata(PdfParseResult parseResult, UploadManualCommand request)
    {
        var metadata = new Dictionary<string, object>
        {
            ["pageCount"] = parseResult.PageCount,
            ["sourceType"] = request.SourceType.ToString(),
            ["uploadedAt"] = DateTime.UtcNow.ToString("O")
        };

        if (!string.IsNullOrEmpty(parseResult.Author))
            metadata["author"] = parseResult.Author;

        if (!string.IsNullOrEmpty(request.Version))
            metadata["version"] = request.Version;

        return System.Text.Json.JsonSerializer.Serialize(metadata);
    }
}
