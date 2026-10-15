using MediatR;

namespace Loremaster.Application.Features.Ai.Queries.CheckAiHealth;

/// <summary>
/// MediatR query that asks whether the Genkit AI service is reachable and healthy.
/// </summary>
/// <param name="ServiceLabel">Human-readable label included in the response (e.g. "Genkit AI").</param>
public record CheckAiHealthQuery(string ServiceLabel = "Genkit AI") : IRequest<CheckAiHealthResult>;

/// <summary>
/// Result returned by <see cref="CheckAiHealthQueryHandler"/>.
/// </summary>
/// <param name="IsHealthy">Whether the service responded successfully.</param>
/// <param name="Service">Human-readable service identifier passed through from the query.</param>
/// <param name="Timestamp">UTC timestamp at which the check was performed.</param>
public record CheckAiHealthResult(bool IsHealthy, string Service, DateTime Timestamp);
