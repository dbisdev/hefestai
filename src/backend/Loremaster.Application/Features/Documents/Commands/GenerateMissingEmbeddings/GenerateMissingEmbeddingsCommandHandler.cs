using Loremaster.Application.Common.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Loremaster.Application.Features.Documents.Commands.GenerateMissingEmbeddings;

/// <summary>
/// Handler for GenerateMissingEmbeddingsCommand.
/// Processes documents without embeddings in batches and generates embeddings via the AI service.
/// </summary>
public class GenerateMissingEmbeddingsCommandHandler 
    : IRequestHandler<GenerateMissingEmbeddingsCommand, GenerateMissingEmbeddingsResult>
{
    private readonly IDocumentRepository _documentRepository;
    private readonly IEmbeddingService _embeddingService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<GenerateMissingEmbeddingsCommandHandler> _logger;

    public GenerateMissingEmbeddingsCommandHandler(
        IDocumentRepository documentRepository,
        IEmbeddingService embeddingService,
        IUnitOfWork unitOfWork,
        ILogger<GenerateMissingEmbeddingsCommandHandler> logger)
    {
        _documentRepository = documentRepository;
        _embeddingService = embeddingService;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<GenerateMissingEmbeddingsResult> Handle(
        GenerateMissingEmbeddingsCommand request, 
        CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Starting embedding generation for owner {OwnerId}, batch size: {BatchSize}, max: {MaxDocuments}",
            request.OwnerId, request.BatchSize, request.MaxDocuments);

        var errors = new List<string>();
        var totalProcessed = 0;
        var successCount = 0;
        var failureCount = 0;

        // Calculate effective limit
        var effectiveLimit = request.MaxDocuments > 0 ? request.MaxDocuments : int.MaxValue;

        while (totalProcessed < effectiveLimit)
        {
            // Get next batch of documents without embeddings
            var batchLimit = Math.Min(request.BatchSize, effectiveLimit - totalProcessed);
            var documents = await _documentRepository.GetDocumentsWithoutEmbeddingAsync(
                request.OwnerId,
                batchLimit,
                cancellationToken);

            if (documents.Count == 0)
            {
                _logger.LogInformation("No more documents without embeddings found");
                break;
            }

            _logger.LogDebug("Processing batch of {Count} documents", documents.Count);

            // Filter by GameSystemId if specified
            var filteredDocs = documents.AsEnumerable();
            if (request.GameSystemId.HasValue)
            {
                filteredDocs = filteredDocs.Where(d => d.GameSystemId == request.GameSystemId.Value);
            }
            if (request.ProjectId.HasValue)
            {
                filteredDocs = filteredDocs.Where(d => d.ProjectId == request.ProjectId.Value);
            }

            var docsToProcess = filteredDocs.ToList();
            if (docsToProcess.Count == 0)
            {
                _logger.LogDebug("No documents match filters in this batch, continuing...");
                totalProcessed += documents.Count;
                continue;
            }

            // Get content for batch embedding
            var contents = docsToProcess.Select(d => d.Content).ToList();

            try
            {
                // Generate embeddings in batch
                var embeddingsResult = await _embeddingService.GetEmbeddingsAsync(contents, cancellationToken);

                // Assign embeddings to documents
                for (var i = 0; i < docsToProcess.Count && i < embeddingsResult.Embeddings.Length; i++)
                {
                    try
                    {
                        docsToProcess[i].SetEmbedding(embeddingsResult.Embeddings[i]);
                        await _documentRepository.UpdateAsync(docsToProcess[i], cancellationToken);
                        successCount++;
                        
                        _logger.LogDebug(
                            "Generated embedding for document {DocumentId}: {Title}",
                            docsToProcess[i].Id, docsToProcess[i].Title);
                    }
                    catch (Exception ex)
                    {
                        failureCount++;
                        var errorMsg = $"Failed to update document {docsToProcess[i].Id}: {ex.Message}";
                        errors.Add(errorMsg);
                        _logger.LogWarning(ex, "Failed to update document {DocumentId}", docsToProcess[i].Id);
                    }
                }

                // Save batch
                await _unitOfWork.SaveChangesAsync(cancellationToken);
                
                _logger.LogInformation(
                    "Batch completed: {Success} success, {Failed} failed",
                    docsToProcess.Count - failureCount, failureCount);
            }
            catch (Exception ex)
            {
                // Batch embedding failed - try individual documents
                _logger.LogWarning(ex, "Batch embedding failed, falling back to individual processing");

                foreach (var doc in docsToProcess)
                {
                    try
                    {
                        var embedding = await _embeddingService.GetEmbeddingAsync(doc.Content, cancellationToken);
                        doc.SetEmbedding(embedding);
                        await _documentRepository.UpdateAsync(doc, cancellationToken);
                        successCount++;
                    }
                    catch (Exception docEx)
                    {
                        failureCount++;
                        var errorMsg = $"Failed to embed document {doc.Id} ({doc.Title}): {docEx.Message}";
                        errors.Add(errorMsg);
                        _logger.LogWarning(docEx, "Failed to embed document {DocumentId}", doc.Id);
                    }
                }

                await _unitOfWork.SaveChangesAsync(cancellationToken);
            }

            totalProcessed += documents.Count;

            // Small delay between batches to avoid rate limiting
            if (totalProcessed < effectiveLimit)
            {
                await Task.Delay(500, cancellationToken);
            }
        }

        _logger.LogInformation(
            "Embedding generation completed. Processed: {Total}, Success: {Success}, Failed: {Failed}",
            totalProcessed, successCount, failureCount);

        return new GenerateMissingEmbeddingsResult(totalProcessed, successCount, failureCount, errors);
    }
}
