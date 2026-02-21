using System.Text.Json;
using Loremaster.Application.Common.Interfaces;
using Loremaster.Domain.Entities;
using Loremaster.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace Loremaster.Application.Features.EntityGeneration.Services;

public class GenerationTrackingService : IGenerationTrackingService
{
    private readonly IGenerationRequestRepository _generationRequestRepository;
    private readonly IApplicationDbContext _dbContext;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<GenerationTrackingService> _logger;

    public GenerationTrackingService(
        IGenerationRequestRepository generationRequestRepository,
        IApplicationDbContext dbContext,
        IUnitOfWork unitOfWork,
        ILogger<GenerationTrackingService> logger)
    {
        _generationRequestRepository = generationRequestRepository;
        _dbContext = dbContext;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public GenerationRequest CreateRequest(Guid userId, string entityType, string fullPrompt)
    {
        var request = GenerationRequest.Create(
            userId: userId,
            requestType: GenerationRequestType.AiNarrative,
            targetEntityType: entityType,
            campaignId: null,
            inputPrompt: fullPrompt,
            inputParameters: null);

        request.StartProcessing();
        return request;
    }

    public GenerationResult CreateResult(
        Guid generationRequestId,
        string resultJson,
        bool ragContextUsed,
        int ragSourceCount,
        bool hasImage,
        string modelName = DefaultGenerationConstants.ModelName,
        float temperature = DefaultGenerationConstants.Temperature,
        int maxTokens = DefaultGenerationConstants.MaxTokens)
    {
        var structuredOutput = new
        {
            content = resultJson,
            ragContextUsed,
            ragSourceCount,
            hasImage
        };

        var structuredJson = JsonDocument.Parse(JsonSerializer.Serialize(structuredOutput));
        var modelParams = new { temperature, maxTokens, model = modelName };
        var modelParamsJson = JsonDocument.Parse(JsonSerializer.Serialize(modelParams));

        return GenerationResult.Create(
            generationRequestId: generationRequestId,
            resultType: "ai_narrative",
            sequenceOrder: 1,
            rawOutput: null,
            structuredOutput: structuredJson,
            modelName: modelName,
            modelParameters: modelParamsJson,
            tokenUsage: null);
    }

    public async Task PersistAsync(
        GenerationRequest request,
        GenerationResult result,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await _generationRequestRepository.AddAsync(request, cancellationToken);
            await _dbContext.GenerationResults.AddAsync(result, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Successfully persisted GenerationRequest {RequestId} with GenerationResult {ResultId}",
                request.Id, result.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Failed to persist GenerationRequest {RequestId} (UserId: {UserId}, Type: {Type})",
                request.Id, request.UserId, request.TargetEntityType);
        }
    }
}
