using Loremaster.Application.Common.Interfaces;
using MediatR;

namespace Loremaster.Application.Features.Ai.Queries.CheckAiHealth;

/// <summary>
/// Handles <see cref="CheckAiHealthQuery"/> by delegating to <see cref="IAiService.IsHealthyAsync"/>.
/// Keeps infrastructure concerns (HTTP call) in Infrastructure while routing stays in Application.
/// </summary>
public sealed class CheckAiHealthQueryHandler : IRequestHandler<CheckAiHealthQuery, CheckAiHealthResult>
{
    private readonly IAiService _aiService;
    private readonly IDateTimeProvider _dateTime;

    /// <summary>
    /// Initializes a new instance of <see cref="CheckAiHealthQueryHandler"/>.
    /// </summary>
    public CheckAiHealthQueryHandler(IAiService aiService, IDateTimeProvider dateTime)
    {
        _aiService = aiService;
        _dateTime = dateTime;
    }

    /// <inheritdoc/>
    public async Task<CheckAiHealthResult> Handle(
        CheckAiHealthQuery request,
        CancellationToken cancellationToken)
    {
        var isHealthy = await _aiService.IsHealthyAsync(cancellationToken);
        return new CheckAiHealthResult(isHealthy, request.ServiceLabel, _dateTime.UtcNow);
    }
}
