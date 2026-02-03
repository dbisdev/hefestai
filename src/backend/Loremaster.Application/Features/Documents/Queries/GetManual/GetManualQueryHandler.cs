using Loremaster.Application.Common.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Loremaster.Application.Features.Documents.Queries.GetManual;

/// <summary>
/// Handler for GetManualQuery. Retrieves a manual document with its chunk count.
/// </summary>
public class GetManualQueryHandler : IRequestHandler<GetManualQuery, GetManualResult?>
{
    private readonly IDocumentRepository _documentRepository;
    private readonly ILogger<GetManualQueryHandler> _logger;

    public GetManualQueryHandler(
        IDocumentRepository documentRepository,
        ILogger<GetManualQueryHandler> logger)
    {
        _documentRepository = documentRepository;
        _logger = logger;
    }

    /// <summary>
    /// Handles the GetManualQuery by retrieving the manual and its chunk count.
    /// Returns null if the manual is not found or doesn't belong to the game system.
    /// </summary>
    public async Task<GetManualResult?> Handle(GetManualQuery request, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Getting manual {ManualId} for game system {GameSystemId} by owner {OwnerId}",
            request.ManualId, request.GameSystemId, request.OwnerId);

        var result = await _documentRepository.GetManualWithChunkCountAsync(
            request.ManualId,
            request.OwnerId,
            cancellationToken);

        if (result == null)
        {
            _logger.LogWarning("Manual {ManualId} not found for owner {OwnerId}", 
                request.ManualId, request.OwnerId);
            return null;
        }

        var manual = result.Manual;

        // Verify the manual belongs to the specified game system
        if (manual.GameSystemId != request.GameSystemId)
        {
            _logger.LogWarning(
                "Manual {ManualId} belongs to game system {ActualGameSystemId}, not {RequestedGameSystemId}",
                request.ManualId, manual.GameSystemId, request.GameSystemId);
            return null;
        }

        _logger.LogInformation(
            "Found manual {ManualId} with {ChunkCount} chunks",
            request.ManualId, result.ChunkCount);

        return new GetManualResult(
            manual.Id,
            manual.GameSystemId!.Value,
            manual.Title,
            result.ChunkCount,
            manual.SourceType,
            manual.Source,
            manual.CreatedAt,
            manual.UpdatedAt);
    }
}
