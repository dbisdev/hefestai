namespace Loremaster.Domain.Entities;

/// <summary>
/// Junction table linking generation results to RAG sources used
/// </summary>
public class GenerationResultSource
{
    public Guid GenerationResultId { get; private set; }
    public GenerationResult GenerationResult { get; private set; } = null!;

    public Guid RagSourceId { get; private set; }
    public RagSource RagSource { get; private set; } = null!;

    public decimal? RelevanceScore { get; private set; }
    public string? Excerpt { get; private set; }

    private GenerationResultSource() { } // EF Core

    public static GenerationResultSource Create(
        Guid generationResultId,
        Guid ragSourceId,
        decimal? relevanceScore = null,
        string? excerpt = null)
    {
        if (relevanceScore.HasValue && (relevanceScore < 0 || relevanceScore > 1))
            throw new ArgumentOutOfRangeException(nameof(relevanceScore), "Relevance score must be between 0 and 1");

        return new GenerationResultSource
        {
            GenerationResultId = generationResultId,
            RagSourceId = ragSourceId,
            RelevanceScore = relevanceScore,
            Excerpt = excerpt?.Trim()
        };
    }
}
