using Loremaster.Application.Common.Models;

namespace Loremaster.Application.Common.Interfaces;

/// <summary>
/// Generates answers from a language model using pre-retrieved context documents
/// (Retrieval-Augmented Generation).
/// Separated from <see cref="IEmbeddingService"/> to respect the Single Responsibility Principle:
/// embedding vectors and context-grounded generation are distinct concerns.
/// </summary>
public interface IRagGenerationService
{
    /// <summary>
    /// Generates an answer to <paramref name="query"/> using the supplied <paramref name="context"/>
    /// documents as grounding material.
    /// </summary>
    /// <param name="query">The question or instruction to answer.</param>
    /// <param name="context">Pre-retrieved document chunks used as grounding context.</param>
    /// <param name="systemPrompt">Optional system prompt to configure model behaviour.</param>
    /// <param name="temperature">Sampling temperature (0–2). Lower values are more deterministic.</param>
    /// <param name="maxTokens">Maximum tokens for the generated answer.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The generated answer together with optional token-usage statistics.</returns>
    Task<RagGenerateResult> GenerateWithContextAsync(
        string query,
        IEnumerable<string> context,
        string? systemPrompt = null,
        float temperature = 0.3f,
        int maxTokens = 2048,
        CancellationToken cancellationToken = default);
}
