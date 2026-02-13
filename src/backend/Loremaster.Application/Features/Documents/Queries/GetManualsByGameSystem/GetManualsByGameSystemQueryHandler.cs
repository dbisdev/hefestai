using Loremaster.Application.Common.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Loremaster.Application.Features.Documents.Queries.GetManualsByGameSystem;

/// <summary>
/// Handler for GetManualsByGameSystemQuery. 
/// Retrieves all manuals for a game system with their chunk counts.
/// </summary>
public class GetManualsByGameSystemQueryHandler 
    : IRequestHandler<GetManualsByGameSystemQuery, IReadOnlyList<ManualSummaryResult>>
{
    private readonly IDocumentRepository _documentRepository;
    private readonly ILogger<GetManualsByGameSystemQueryHandler> _logger;

    public GetManualsByGameSystemQueryHandler(
        IDocumentRepository documentRepository,
        ILogger<GetManualsByGameSystemQueryHandler> logger)
    {
        _documentRepository = documentRepository;
        _logger = logger;
    }

    /// <summary>
    /// Handles the query by retrieving all manuals for the specified game system.
    /// </summary>
    public async Task<IReadOnlyList<ManualSummaryResult>> Handle(
        GetManualsByGameSystemQuery request, 
        CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Getting manuals for game system {GameSystemId} by owner {OwnerId}",
            request.GameSystemId, request.OwnerId);

        var manuals = await _documentRepository.GetManualsByGameSystemIdAsync(
            request.GameSystemId,
            request.OwnerId,
            cancellationToken);

        _logger.LogInformation(
            "Found {ManualCount} manuals for game system {GameSystemId}",
            manuals.Count, request.GameSystemId);

        return manuals.Select(m => new ManualSummaryResult(
            m.Manual.Id,
            m.Manual.GameSystemId!.Value,
            m.Manual.Title,
            m.ChunkCount,
            m.Manual.SourceType,
            m.Manual.Source,
            m.Manual.CreatedAt))
            .ToList();
    }
}
