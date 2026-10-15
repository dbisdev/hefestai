using Loremaster.Application.Common.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Loremaster.Application.Features.EntityGeneration.Commands.TestAi;

/// <summary>
/// Handles the <see cref="TestAiCommand"/> by delegating to the AI service
/// and mapping the result to a <see cref="TestAiResult"/>.
/// Keeps business logic out of the controller layer.
/// </summary>
public sealed class TestAiCommandHandler : IRequestHandler<TestAiCommand, TestAiResult>
{
    private readonly IAiService _aiService;
    private readonly ILogger<TestAiCommandHandler> _logger;

    /// <summary>
    /// Initializes a new instance of <see cref="TestAiCommandHandler"/>.
    /// </summary>
    /// <param name="aiService">AI service used to generate text.</param>
    /// <param name="logger">Logger for diagnostic output.</param>
    public TestAiCommandHandler(IAiService aiService, ILogger<TestAiCommandHandler> logger)
    {
        _aiService = aiService;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<TestAiResult> Handle(TestAiCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Testing AI service with prompt: {Prompt}", request.Prompt);

        try
        {
            var result = await _aiService.GenerateTextAsync(
                request.Prompt,
                request.SystemPrompt,
                request.Temperature,
                request.MaxTokens,
                cancellationToken);

            var usage = result.Usage is not null
                ? new TestAiTokenUsage(
                    result.Usage.PromptTokens,
                    result.Usage.CompletionTokens,
                    result.Usage.TotalTokens)
                : null;

            return new TestAiResult(Success: true, Text: result.Text, Error: null, Usage: usage);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to test AI service");
            return new TestAiResult(Success: false, Text: null, Error: ex.Message, Usage: null);
        }
    }
}
