namespace Loremaster.Application.Features.EntityGeneration.DTOs;

public abstract record GenerationResponseBase
{
    public bool Success { get; init; }
    public string? Error { get; init; }
    public bool RagContextUsed { get; init; }
    public int RagSourceCount { get; init; }
    public Guid? GenerationRequestId { get; init; }
}
