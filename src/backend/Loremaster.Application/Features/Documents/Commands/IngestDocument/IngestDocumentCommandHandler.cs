using Loremaster.Application.Common.Interfaces;
using Loremaster.Domain.Entities;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Loremaster.Application.Features.Documents.Commands.IngestDocument;

public class IngestDocumentCommandHandler : IRequestHandler<IngestDocumentCommand, IngestDocumentResult>
{
    private readonly IDocumentRepository _documentRepository;
    private readonly IEmbeddingService _embeddingService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<IngestDocumentCommandHandler> _logger;

    public IngestDocumentCommandHandler(
        IDocumentRepository documentRepository,
        IEmbeddingService embeddingService,
        IUnitOfWork unitOfWork,
        ILogger<IngestDocumentCommandHandler> logger)
    {
        _documentRepository = documentRepository;
        _embeddingService = embeddingService;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<IngestDocumentResult> Handle(IngestDocumentCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Ingesting document: {Title} for owner {OwnerId}", request.Title, request.OwnerId);

        // Create document entity
        var document = Document.Create(
            request.Title,
            request.Content,
            request.OwnerId,
            request.Source,
            request.Metadata);

        await _documentRepository.AddAsync(document, cancellationToken);

        bool embeddingGenerated = false;
        int? embeddingDimensions = null;

        // Generate embedding if requested
        if (request.GenerateEmbedding)
        {
            try
            {
                _logger.LogDebug("Generating embedding for document {DocumentId}", document.Id);
                
                var embedding = await _embeddingService.GetEmbeddingAsync(request.Content, cancellationToken);
                document.SetEmbedding(embedding);
                
                embeddingGenerated = true;
                embeddingDimensions = embedding.Length;
                
                _logger.LogInformation("Embedding generated for document {DocumentId}, dimensions: {Dimensions}", 
                    document.Id, embeddingDimensions);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to generate embedding for document {DocumentId}. Document saved without embedding.", 
                    document.Id);
            }
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new IngestDocumentResult(document.Id, embeddingGenerated, embeddingDimensions);
    }
}
