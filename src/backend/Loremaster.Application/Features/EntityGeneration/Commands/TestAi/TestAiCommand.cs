using MediatR;

namespace Loremaster.Application.Features.EntityGeneration.Commands.TestAi;

/// <summary>
/// Command to test the AI service connectivity and text generation.
/// Used for health checks and diagnostic purposes.
/// </summary>
/// <param name="Prompt">The prompt to send to the AI service.</param>
/// <param name="SystemPrompt">Optional system prompt to configure AI behaviour.</param>
/// <param name="Temperature">Sampling temperature (0.0–1.0). Defaults to 0.7.</param>
/// <param name="MaxTokens">Maximum tokens to generate. Defaults to 256.</param>
public record TestAiCommand(
    string Prompt,
    string? SystemPrompt,
    float Temperature,
    int MaxTokens) : IRequest<TestAiResult>;

/// <summary>
/// Result of the AI test command.
/// </summary>
/// <param name="Success">Whether the AI call completed without errors.</param>
/// <param name="Text">Generated text, if successful.</param>
/// <param name="Error">Error message, if unsuccessful.</param>
/// <param name="Usage">Token usage information, if returned by the service.</param>
public record TestAiResult(
    bool Success,
    string? Text,
    string? Error,
    TestAiTokenUsage? Usage);

/// <summary>
/// Token consumption details returned by the AI service.
/// </summary>
/// <param name="PromptTokens">Tokens used by the prompt.</param>
/// <param name="CompletionTokens">Tokens generated in the completion.</param>
/// <param name="TotalTokens">Total tokens consumed.</param>
public record TestAiTokenUsage(
    int PromptTokens,
    int CompletionTokens,
    int TotalTokens);
