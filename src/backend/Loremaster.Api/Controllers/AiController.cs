using System.Security.Claims;
using System.Text.Json;
using Loremaster.Application.Common.Interfaces;
using Loremaster.Domain.Entities;
using Loremaster.Domain.Enums;
using Loremaster.Domain.ValueObjects;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Loremaster.Api.Controllers;

/// <summary>
/// AI Controller for generating campaign entities using RAG-enhanced AI generation.
/// Leverages game system manuals for lore-accurate content generation.
/// Creates GenerationRequest/GenerationResult records for full traceability.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class AiController : ControllerBase
{
    private readonly IAiService _aiService;
    private readonly IRagContextProvider _ragContextProvider;
    private readonly IEmbeddingService _embeddingService;
    private readonly IGenerationRequestRepository _generationRequestRepository;
    private readonly IEntityTemplateRepository _entityTemplateRepository;
    private readonly IApplicationDbContext _dbContext;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<AiController> _logger;

    /// <summary>
    /// Default model name for Gemini used in generation.
    /// </summary>
    private const string DefaultModelName = "gemini-2.0-flash";

    public AiController(
        IAiService aiService,
        IRagContextProvider ragContextProvider,
        IEmbeddingService embeddingService,
        IGenerationRequestRepository generationRequestRepository,
        IEntityTemplateRepository entityTemplateRepository,
        IApplicationDbContext dbContext,
        IUnitOfWork unitOfWork,
        ILogger<AiController> logger)
    {
        _aiService = aiService;
        _ragContextProvider = ragContextProvider;
        _embeddingService = embeddingService;
        _generationRequestRepository = generationRequestRepository;
        _entityTemplateRepository = entityTemplateRepository;
        _dbContext = dbContext;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    /// <summary>
    /// Gets the current authenticated user's ID from JWT claims.
    /// </summary>
    /// <returns>User ID as Guid.</returns>
    /// <exception cref="UnauthorizedAccessException">If user token is invalid.</exception>
    private Guid GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst("sub")?.Value
            ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        
        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
        {
            throw new UnauthorizedAccessException("Invalid user token");
        }

        return userId;
    }

    /// <summary>
    /// Retrieves a confirmed template for the given entity type and game system.
    /// Returns null if no confirmed template exists.
    /// </summary>
    /// <param name="gameSystemId">The game system ID.</param>
    /// <param name="userId">The user ID (owner).</param>
    /// <param name="entityTypeName">The entity type name (e.g., "character", "npc").</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The confirmed template or null if not found.</returns>
    private async Task<EntityTemplate?> GetConfirmedTemplateAsync(
        Guid? gameSystemId,
        Guid userId,
        string entityTypeName,
        CancellationToken cancellationToken)
    {
        if (!gameSystemId.HasValue)
            return null;

        var template = await _entityTemplateRepository.GetConfirmedTemplateForEntityTypeAsync(
            gameSystemId.Value,
            userId,
            entityTypeName,
            cancellationToken);

        if (template != null)
        {
            _logger.LogDebug(
                "Found confirmed template {TemplateId} for entity type '{EntityType}' in game system {GameSystemId}",
                template.Id, entityTypeName, gameSystemId);
        }

        return template;
    }

    /// <summary>
    /// Builds an example JSON format string from template field definitions.
    /// Used to guide LLM generation with the correct schema.
    /// </summary>
    /// <param name="fields">The field definitions from the template.</param>
    /// <returns>A JSON example string formatted for the LLM prompt.</returns>
    private static string BuildExampleJsonFromTemplate(IReadOnlyList<FieldDefinition> fields)
    {
        var orderedFields = fields.OrderBy(f => f.Order).ToList();
        var exampleObject = new Dictionary<string, object>();

        foreach (var field in orderedFields)
        {
            var exampleValue = GenerateExampleValue(field);
            exampleObject[field.Name] = exampleValue;
        }

        // Serialize with indentation for readability
        var options = new JsonSerializerOptions
        {
            WriteIndented = true
        };

        return JsonSerializer.Serialize(exampleObject, options);
    }

    /// <summary>
    /// Generates an example value for a field based on its type and constraints.
    /// </summary>
    /// <param name="field">The field definition.</param>
    /// <returns>An example value appropriate for the field type.</returns>
    private static object GenerateExampleValue(FieldDefinition field)
    {
        return field.FieldType switch
        {
            FieldType.Text => GenerateTextExample(field),
            FieldType.TextArea => GenerateTextAreaExample(field),
            FieldType.Number => GenerateNumberExample(field),
            FieldType.Boolean => false,
            FieldType.Select => GenerateSelectExample(field),
            FieldType.MultiSelect => GenerateMultiSelectExample(field),
            FieldType.Date => DateTime.UtcNow.ToString("yyyy-MM-dd"),
            FieldType.Url => "https://example.com/image.png",
            FieldType.Json => GenerateJsonExample(field),
            _ => $"<{field.DisplayName}>"
        };
    }

    /// <summary>
    /// Generates a text example based on field properties.
    /// </summary>
    private static string GenerateTextExample(FieldDefinition field)
    {
        // Use description as hint for what kind of value is expected
        if (!string.IsNullOrEmpty(field.Description))
            return $"<{field.Description}>";
        
        return $"<{field.DisplayName}>";
    }

    /// <summary>
    /// Generates a textarea example (typically longer text).
    /// </summary>
    private static string GenerateTextAreaExample(FieldDefinition field)
    {
        if (!string.IsNullOrEmpty(field.Description))
            return $"<{field.Description} - 2-3 paragraphs>";
        
        return $"<{field.DisplayName} - detailed description>";
    }

    /// <summary>
    /// Generates a number example based on min/max constraints.
    /// </summary>
    private static object GenerateNumberExample(FieldDefinition field)
    {
        if (field.MinValue.HasValue && field.MaxValue.HasValue)
        {
            // Return middle value
            var mid = (field.MinValue.Value + field.MaxValue.Value) / 2;
            return Math.Round(mid);
        }
        
        if (field.MinValue.HasValue)
            return field.MinValue.Value;
        
        if (field.MaxValue.HasValue)
            return field.MaxValue.Value / 2;
        
        return 50; // Default example number
    }

    /// <summary>
    /// Generates a select example from available options.
    /// </summary>
    private static string GenerateSelectExample(FieldDefinition field)
    {
        var options = field.GetOptions();
        return options.FirstOrDefault() ?? $"<{field.DisplayName} option>";
    }

    /// <summary>
    /// Generates a multi-select example from available options.
    /// </summary>
    private static object GenerateMultiSelectExample(FieldDefinition field)
    {
        var options = field.GetOptions();
        // Return first two options as example
        return options.Take(2).ToList();
    }

    /// <summary>
    /// Generates a JSON example (nested object placeholder).
    /// </summary>
    private static object GenerateJsonExample(FieldDefinition field)
    {
        // Return a placeholder object structure
        return new Dictionary<string, object>
        {
            ["key"] = "value",
            ["nested"] = new Dictionary<string, object>
            {
                ["property"] = $"<{field.DisplayName} content>"
            }
        };
    }

    /// <summary>
    /// Builds a field description list from template fields for the LLM prompt.
    /// </summary>
    /// <param name="fields">The field definitions from the template.</param>
    /// <returns>A formatted string describing each field.</returns>
    private static string BuildFieldDescriptions(IReadOnlyList<FieldDefinition> fields)
    {
        var orderedFields = fields.OrderBy(f => f.Order).ToList();
        var descriptions = new List<string>();

        foreach (var field in orderedFields)
        {
            var desc = $"- {field.Name}: {GetFieldTypeDescription(field)}";
            
            if (!string.IsNullOrEmpty(field.Description))
                desc += $" ({field.Description})";
            
            if (field.IsRequired)
                desc += " [REQUIRED]";
            
            descriptions.Add(desc);
        }

        return string.Join("\n", descriptions);
    }

    /// <summary>
    /// Gets a human-readable description of the field type and constraints.
    /// </summary>
    private static string GetFieldTypeDescription(FieldDefinition field)
    {
        return field.FieldType switch
        {
            FieldType.Text => "Short text",
            FieldType.TextArea => "Long text/description",
            FieldType.Number when field.MinValue.HasValue && field.MaxValue.HasValue 
                => $"Number ({field.MinValue}-{field.MaxValue})",
            FieldType.Number when field.MinValue.HasValue 
                => $"Number (min: {field.MinValue})",
            FieldType.Number when field.MaxValue.HasValue 
                => $"Number (max: {field.MaxValue})",
            FieldType.Number => "Number",
            FieldType.Boolean => "Boolean (true/false)",
            FieldType.Select => $"One of: [{string.Join(", ", field.GetOptions())}]",
            FieldType.MultiSelect => $"Array of: [{string.Join(", ", field.GetOptions())}]",
            FieldType.Date => "Date (YYYY-MM-DD)",
            FieldType.Url => "URL string",
            FieldType.Json => "Nested JSON object",
            _ => "Value"
        };
    }

    /// <summary>
    /// Generates entity content using RAG-enhanced AI or fallback to basic generation.
    /// </summary>
    /// <param name="gameSystemId">Optional game system ID for RAG context.</param>
    /// <param name="userId">The owner/user ID.</param>
    /// <param name="entityType">Entity type name for RAG query (e.g., "character", "npc").</param>
    /// <param name="additionalContext">Additional context for RAG search query.</param>
    /// <param name="systemPrompt">System prompt for RAG-enhanced generation.</param>
    /// <param name="userQuery">User query for RAG-enhanced generation.</param>
    /// <param name="fallbackPrompt">Fallback prompt if no RAG context available.</param>
    /// <param name="fallbackSystemPrompt">Fallback system prompt.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Tuple of (generated JSON, RAG context chunks used, full prompt sent to LLM).</returns>
    private async Task<(string Json, IReadOnlyList<RagContextChunk> RagContext, string FullPrompt)> GenerateWithRagAsync(
        Guid? gameSystemId,
        Guid userId,
        string entityType,
        string additionalContext,
        string systemPrompt,
        string userQuery,
        string fallbackPrompt,
        string fallbackSystemPrompt,
        CancellationToken cancellationToken)
    {
        IReadOnlyList<RagContextChunk> ragContext = Array.Empty<RagContextChunk>();
        
        // Step 1: Retrieve RAG context if game system ID is provided
        if (gameSystemId.HasValue)
        {
            _logger.LogDebug("Retrieving RAG context for {EntityType} generation from game system {GameSystemId}", 
                entityType, gameSystemId);
            
            ragContext = await _ragContextProvider.GetContextForEntityGenerationAsync(
                gameSystemId.Value,
                userId,
                entityTypeName: entityType,
                additionalContext: additionalContext,
                maxChunks: 7,
                cancellationToken: cancellationToken);

            _logger.LogDebug("Retrieved {ChunkCount} RAG context chunks for {EntityType} generation", 
                ragContext.Count, entityType);
        }

        // Step 2: Generate using RAG context or fallback
        string resultJson;
        string fullPrompt;

        if (ragContext.Any())
        {
            // RAG-enhanced generation
            var contextTexts = ragContext.Select(c => c.Content).ToList();
            
            _logger.LogDebug("Using RAG-enhanced generation with {ContextCount} context chunks", contextTexts.Count);
            
            // Build full prompt for traceability
            fullPrompt = $"[SYSTEM]\n{systemPrompt}\n\n[USER]\n{userQuery}\n\n[RAG CONTEXT]\n{string.Join("\n---\n", contextTexts)}";
            
            var ragResult = await _embeddingService.GenerateWithContextAsync(
                query: userQuery,
                context: contextTexts,
                systemPrompt: systemPrompt,
                temperature: 0.8f,
                maxTokens: 2048,
                cancellationToken: cancellationToken);

            resultJson = ragResult.Answer;
        }
        else
        {
            // Fallback: Basic generation without RAG context
            _logger.LogDebug("No RAG context available for {EntityType}, using basic generation", entityType);
            
            // Build full prompt for traceability
            fullPrompt = $"[SYSTEM]\n{fallbackSystemPrompt}\n\n[USER]\n{fallbackPrompt}";
            
            var textResult = await _aiService.GenerateJsonAsync(
                fallbackPrompt,
                fallbackSystemPrompt,
                0.8f,
                2048,
                cancellationToken);

            resultJson = textResult.Json;
        }

        return (resultJson, ragContext, fullPrompt);
    }

    /// <summary>
    /// Creates a GenerationRequest for tracking AI generation.
    /// </summary>
    /// <param name="userId">The user initiating the generation.</param>
    /// <param name="entityType">Type of entity being generated (e.g., "character", "npc").</param>
    /// <param name="inputPrompt">The user's input prompt or generation parameters.</param>
    /// <param name="inputParameters">Optional structured input parameters.</param>
    /// <returns>A new GenerationRequest in Processing status.</returns>
    private GenerationRequest CreateGenerationRequest(
        Guid userId,
        string entityType,
        string inputPrompt,
        JsonDocument? inputParameters = null)
    {
        var request = GenerationRequest.Create(
            userId: userId,
            requestType: GenerationRequestType.AiNarrative,
            targetEntityType: entityType,
            campaignId: null, // AiController is not campaign-scoped
            inputPrompt: inputPrompt,
            inputParameters: inputParameters);

        request.StartProcessing();
        return request;
    }

    /// <summary>
    /// Creates a GenerationResult record from the generation output.
    /// </summary>
    /// <param name="generationRequestId">ID of the parent GenerationRequest.</param>
    /// <param name="resultJson">The generated JSON content.</param>
    /// <param name="ragContextUsed">Whether RAG context was used.</param>
    /// <param name="ragSourceCount">Number of RAG sources used.</param>
    /// <param name="hasImage">Whether an image was generated.</param>
    /// <returns>A new GenerationResult.</returns>
    private static GenerationResult CreateGenerationResult(
        Guid generationRequestId,
        string resultJson,
        bool ragContextUsed,
        int ragSourceCount,
        bool hasImage)
    {
        // Build structured output
        var structuredOutput = new
        {
            content = resultJson,
            ragContextUsed,
            ragSourceCount,
            hasImage
        };

        var structuredJson = JsonDocument.Parse(JsonSerializer.Serialize(structuredOutput));

        // Build model parameters
        var modelParams = new
        {
            temperature = 0.8f,
            maxTokens = 2048,
            model = DefaultModelName
        };
        var modelParamsJson = JsonDocument.Parse(JsonSerializer.Serialize(modelParams));

        return GenerationResult.Create(
            generationRequestId: generationRequestId,
            resultType: "ai_narrative",
            sequenceOrder: 1,
            rawOutput: null,
            structuredOutput: structuredJson,
            modelName: DefaultModelName,
            modelParameters: modelParamsJson,
            tokenUsage: null);
    }

    /// <summary>
    /// Persists the GenerationRequest to the database.
    /// Non-blocking: failures are logged but don't stop the main operation.
    /// </summary>
    private async Task PersistGenerationRequestAsync(
        GenerationRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            await _generationRequestRepository.AddAsync(request, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to persist GenerationRequest {RequestId}", request.Id);
            // Don't throw - generation tracking failure shouldn't fail the main operation
        }
    }

    /// <summary>
    /// Persists the GenerationRequest and GenerationResult to the database.
    /// Non-blocking: failures are logged but don't stop the main operation.
    /// </summary>
    private async Task PersistGenerationRequestWithResultAsync(
        GenerationRequest request,
        GenerationResult result,
        CancellationToken cancellationToken)
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
            // Don't throw - generation tracking failure shouldn't fail the main operation
        }
    }

    /// <summary>
    /// Generate a character with stats and description using RAG-enhanced AI.
    /// Retrieves relevant lore from game system manuals to ensure lore-accurate generation.
    /// Uses confirmed template field definitions for JSON schema when available.
    /// </summary>
    /// <param name="request">Character generation parameters including game system context.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Generated character data with optional image.</returns>
    [HttpPost("generate/character")]
    [ProducesResponseType(typeof(CharacterGenerationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<CharacterGenerationResponse>> GenerateCharacter(
        [FromBody] CharacterGenerationRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var userId = GetCurrentUserId();
            
            _logger.LogInformation(
                "Generating character: Species={Species}, Role={Role}, GameSystemId={GameSystemId}", 
                request.Species, request.Role, request.GameSystemId);

            // Step 1: Check for confirmed template to use its field definitions
            var template = await GetConfirmedTemplateAsync(
                request.GameSystemId,
                userId,
                "character",
                cancellationToken);

            string exampleJson;
            string fieldDescriptions;

            if (template != null)
            {
                var fields = template.GetFieldDefinitions();
                var statsJson = BuildExampleJsonFromTemplate(fields);
                // Wrap template fields inside the standard structure
                exampleJson = $@"{{""name"":""<Character Name>"",""bio"":""<2-3 paragraph backstory>"",""stats"":{statsJson}}}";
                fieldDescriptions = $@"- name: A unique character name fitting the setting
- bio: A 2-3 paragraphs backstory based on the lore
- stats: An object containing the following template fields:
{BuildFieldDescriptions(fields)}";
                _logger.LogDebug("Using template {TemplateId} with {FieldCount} fields for character generation", 
                    template.Id, fields.Count);
            }
            else
            {
                // Fallback example when no template exists
                exampleJson = @"{""name"":""Zephyr-9"",""bio"":""A rogue android..."",""stats"":{
    ""STRENGTH"": 3,
    ""AGILITY"": 4,
    ""WITS"": 4,
    ""EMPATHY"": 5,
    ""SKILLS"": {
      ""MOBILITY"": 2,
      ""OBSERVATION"": 4,
      ""MEDICAL AID"": 4
    },
    ""TALENT"": ""Field Medic"",
    ""GEAR"": ""Medkit, Surgical kit, Four doses of Naproleve"",
    ""CASH"": 1000}}";
                fieldDescriptions = @"- name: A unique character name fitting the setting
- bio: A 2-3 paragraphs backstory based on the lore
- stats: An object with the attributes required by the character creation rules";
            }

            // Step 2: Build additional context from request parameters
            var additionalContext = $"Species: {request.Species}, Role: {request.Role}, Morphology: {request.Morphology}, Style: {request.Attire}";

            // Step 3: Retrieve RAG context from game system manuals (if gameSystemId provided)
            IReadOnlyList<RagContextChunk> ragContext = Array.Empty<RagContextChunk>();
            
            if (request.GameSystemId.HasValue)
            {
                _logger.LogDebug("Retrieving RAG context for character generation from game system {GameSystemId}", request.GameSystemId);
                
                ragContext = await _ragContextProvider.GetContextForEntityGenerationAsync(
                    request.GameSystemId.Value,
                    userId,
                    entityTypeName: "character",
                    additionalContext: additionalContext,
                    maxChunks: 7,
                    cancellationToken: cancellationToken);

                _logger.LogDebug("Retrieved {ChunkCount} RAG context chunks for character generation", ragContext.Count);
            }

            // Step 4: Generate character using RAG context or fallback to basic generation
            string characterJson;
            string fullPrompt; // Store the full prompt sent to LLM

            if (ragContext.Any())
            {
                // RAG-enhanced generation: Use lore context from manuals
                var contextTexts = ragContext.Select(c => c.Content).ToList();
                
                var systemPrompt = @"You are a character generator for a tabletop RPG. 
Use the provided lore context from the game manuals to create a character that fits the game's setting and rules.
The character must be consistent with the game system's lore, species, classes, and mechanics.
Respond only with valid JSON.";

                var userQuery = $@"Based on the game system lore provided, generate a character with these parameters:
Species: {request.Species}
Role: {request.Role}
Morphology: {request.Morphology}
Style: {request.Attire}

Generate a JSON object with the following fields:
{fieldDescriptions}

The character should reflect the game world's themes, factions, and available character options from the lore.

Example format:
{exampleJson}";

                // Capture full prompt for traceability
                fullPrompt = $"[SYSTEM]\n{systemPrompt}\n\n[USER]\n{userQuery}\n\n[RAG CONTEXT]\n{string.Join("\n---\n", contextTexts)}";

                _logger.LogDebug("Using RAG-enhanced generation with {ContextCount} context chunks", contextTexts.Count);
                
                var ragResult = await _embeddingService.GenerateWithContextAsync(
                    query: userQuery,
                    context: contextTexts,
                    systemPrompt: systemPrompt,
                    temperature: 0.8f,
                    maxTokens: 2048,
                    cancellationToken: cancellationToken);

                characterJson = ragResult.Answer;

                _logger.LogDebug(characterJson);
            }
            else
            {
                // Fallback: Basic generation without RAG context
                _logger.LogDebug("No RAG context available, using basic generation");
                
                var fallbackSystemPrompt = "You are a sci-fi character generator. Respond only with valid JSON.";
                var prompt = $@"Generate a sci-fi character based on:
Species: {request.Species}
Role: {request.Role}
Morphology: {request.Morphology}
Style: {request.Attire}

Respond with a JSON object containing the following fields:
{fieldDescriptions}

Example format:
{exampleJson}";

                // Capture full prompt for traceability
                fullPrompt = $"[SYSTEM]\n{fallbackSystemPrompt}\n\n[USER]\n{prompt}";

                var textResult = await _aiService.GenerateJsonAsync(
                    prompt,
                    fallbackSystemPrompt,
                    0.8f,
                    2048,
                    cancellationToken);

                characterJson = textResult.Json;
            }

            // Step 5: Conditionally generate image based on request parameter
            string? imageBase64 = null;
            string? imageUrl = null;
            
            if (request.GenerateImage)
            {
                // Optionally enhance image prompt with style context from RAG
                string imagePromptContext = "";
                
                if (request.GameSystemId.HasValue)
                {
                    var styleContext = await _ragContextProvider.GetStyleContextAsync(
                        request.GameSystemId.Value,
                        userId,
                        "character",
                        cancellationToken);
                    
                    if (styleContext.Any())
                    {
                        imagePromptContext = $" Art style based on: {string.Join(" ", styleContext.Take(2).Select(c => c.Content?.Substring(0, Math.Min(200, c.Content.Length)) ?? ""))}";
                    }
                }

                var imagePrompt = $"High-quality futuristic sci-fi portrait of a {request.Species} {request.Role}, {request.Morphology}, wearing {request.Attire}. Cinematic lighting, detailed face, 8k resolution, professional concept art, black background.{imagePromptContext}";
                var imageResult = await _aiService.GenerateImageAsync(imagePrompt, cancellationToken: cancellationToken);
                imageBase64 = imageResult.ImageBase64;
                imageUrl = imageResult.ImageUrl;
            }

            // Step 6: Create and persist generation tracking records
            var generationRequest = CreateGenerationRequest(userId, "character", fullPrompt);
            generationRequest.Complete();

            var generationResult = CreateGenerationResult(
                generationRequest.Id,
                characterJson,
                ragContext.Any(),
                ragContext.Count,
                imageBase64 != null || imageUrl != null);

            await PersistGenerationRequestWithResultAsync(generationRequest, generationResult, cancellationToken);

            _logger.LogInformation(
                "Character generation completed. GenerationRequestId: {GenerationRequestId}",
                generationRequest.Id);

            return Ok(new CharacterGenerationResponse
            {
                Success = true,
                CharacterJson = characterJson,
                ImageBase64 = imageBase64,
                ImageUrl = imageUrl,
                RagContextUsed = ragContext.Any(),
                RagSourceCount = ragContext.Count,
                GenerationRequestId = generationRequest.Id
            });
        }
        catch (UnauthorizedAccessException)
        {
            return Unauthorized();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate character");
            return StatusCode(500, new CharacterGenerationResponse
            {
                Success = false,
                Error = ex.Message
            });
        }
    }

    /// <summary>
    /// Generate a solar system with planets using RAG-enhanced AI.
    /// Retrieves relevant lore from game system manuals to ensure lore-accurate generation.
    /// Uses confirmed template field definitions for JSON schema when available.
    /// </summary>
    /// <param name="request">Solar system generation parameters including game system context.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Generated solar system data with optional image.</returns>
    [HttpPost("generate/solar-system")]
    [ProducesResponseType(typeof(SolarSystemGenerationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<SolarSystemGenerationResponse>> GenerateSolarSystem(
        [FromBody] SolarSystemGenerationRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var userId = GetCurrentUserId();
            
            _logger.LogInformation(
                "Generating solar system: SpectralClass={SpectralClass}, Planets={PlanetCount}, GameSystemId={GameSystemId}", 
                request.SpectralClass, request.PlanetCount, request.GameSystemId);

            // Check for confirmed template to use its field definitions
            var template = await GetConfirmedTemplateAsync(
                request.GameSystemId,
                userId,
                "solar_system",
                cancellationToken);

            string exampleJson;
            string fieldDescriptions;

            if (template != null)
            {
                var fields = template.GetFieldDefinitions();
                var statsJson = BuildExampleJsonFromTemplate(fields);
                // Wrap template fields inside the standard structure
                exampleJson = $@"{{""name"":""<System Name>"",""description"":""<2-3 paragraph description>"",""stats"":{statsJson}}}";
                fieldDescriptions = $@"- name: A unique star system name fitting the setting
- description: A 2-3 paragraph description
- stats: An object containing the following template fields:
{BuildFieldDescriptions(fields)}";
                _logger.LogDebug("Using template {TemplateId} for solar-system generation", template.Id);
            }
            else
            {
                exampleJson = @"{""name"":""Nexus Prime"",""description"":""A binary system..."",""stats"":{""planets"": 
                [{
    ""orbital_position"": 1,
    ""type"": ""Terrestrial"",
    ""name"": ""LV-426"",        
    ""size"": 4000,        
    ""gravity"": 0.2,        
    ""atmosphere"": ""Thin, unbreathable"",        
    ""temperature"": ""Hot"",        
    ""features"": ""Barren, cratered surface"",        
    ""resources"": ""Trace amounts of common metals""
  }]}}";
                fieldDescriptions = @"- name: A unique star system name fitting the setting
- description: A 2-3 paragraph description
- stats: An object with planets array and other system data";
            }

            // Build context for RAG query
            var additionalContext = $"Star spectral class: {request.SpectralClass}, Planet count: {request.PlanetCount}";

            // RAG-enhanced system prompt
            var systemPrompt = @"You are a star system generator for a tabletop RPG.
Use the provided lore context from the game manuals to create a star system for campaing that fits the game's setting.
The system must be consistent with the game system's lore, factions, and cosmic geography.
Respond only with valid JSON.";

            // RAG user query
            var userQuery = $@"Based on the game system lore provided, generate a star system with these parameters:
Star Spectral Class: {request.SpectralClass}
Number of Planets: {request.PlanetCount}

Generate a JSON object with the following fields:
{fieldDescriptions}

The system should reflect the game world's cosmic themes and known regions from the lore.

Example format:
{exampleJson}";

            // Fallback prompts
            var fallbackPrompt = $@"Create a futuristic star system with {request.PlanetCount} planets orbiting a {request.SpectralClass} class star.
Provide a unique sci-fi name, a brief overview of the system, and a name for each planet.

Respond with a JSON object containing the following fields:
{fieldDescriptions}

Example format:
{exampleJson}";

            var fallbackSystemPrompt = "You are a sci-fi world builder. Respond only with valid JSON.";

            // Generate with RAG
            var (systemJson, ragContext, fullPrompt) = await GenerateWithRagAsync(
                request.GameSystemId,
                userId,
                entityType: "solar-system",
                additionalContext: additionalContext,
                systemPrompt: systemPrompt,
                userQuery: userQuery,
                fallbackPrompt: fallbackPrompt,
                fallbackSystemPrompt: fallbackSystemPrompt,
                cancellationToken);

            // Conditionally generate image based on request parameter
            string? imageBase64 = null;
            string? imageUrl = null;
            
            if (request.GenerateImage)
            {
                // Optionally enhance image prompt with style context from RAG
                string imagePromptContext = "";
                
                if (request.GameSystemId.HasValue)
                {
                    var styleContext = await _ragContextProvider.GetStyleContextAsync(
                        request.GameSystemId.Value,
                        userId,
                        "solar-system",
                        cancellationToken);
                    
                    if (styleContext.Any())
                    {
                        imagePromptContext = $" Art style based on: {string.Join(" ", styleContext.Take(2).Select(c => c.Content?.Substring(0, Math.Min(200, c.Content.Length)) ?? ""))}";
                    }
                }

                // Add the description generated by the LLM to the image prompt for better coherence between text and image (if description is present in the generated JSON)
                string descriptionForImage = "";
                try
                {
                    // Clean markdown code blocks from JSON response before parsing
                    var cleanedJson = systemJson.Trim();
                    if (cleanedJson.StartsWith("```json"))
                        cleanedJson = cleanedJson[7..];
                    else if (cleanedJson.StartsWith("```"))
                        cleanedJson = cleanedJson[3..];
                    if (cleanedJson.EndsWith("```"))
                        cleanedJson = cleanedJson[..^3];
                    cleanedJson = cleanedJson.Trim();

                    var jsonDoc = JsonDocument.Parse(cleanedJson);
                    if (jsonDoc.RootElement.TryGetProperty("description", out var descriptionElement))
                    {
                        descriptionForImage = descriptionElement.GetString() ?? "";
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogDebug(ex, "Failed to extract description from generated system JSON");
                }
                
                if (!string.IsNullOrEmpty(descriptionForImage))
                {
                    imagePromptContext += $" Description: {descriptionForImage}";
                }

                var imagePrompt = $"Breathtaking wide-angle cinematic view of a {request.SpectralClass}-type star solar system. Visible planets orbiting, vibrant cosmic nebulas in background, high detail, photorealistic space photography, sci-fi concept art, deep blacks, vivid colors. {imagePromptContext}";
                var imageResult = await _aiService.GenerateImageAsync(imagePrompt, cancellationToken: cancellationToken);
                imageBase64 = imageResult.ImageBase64;
                imageUrl = imageResult.ImageUrl;
            }

            // Create and persist generation tracking records
            var generationRequest = CreateGenerationRequest(userId, "solar-system", fullPrompt);
            generationRequest.Complete();

            var generationResult = CreateGenerationResult(
                generationRequest.Id,
                systemJson,
                ragContext.Any(),
                ragContext.Count,
                imageBase64 != null || imageUrl != null);

            await PersistGenerationRequestWithResultAsync(generationRequest, generationResult, cancellationToken);

            _logger.LogInformation(
                "Solar system generation completed. GenerationRequestId: {GenerationRequestId}",
                generationRequest.Id);

            return Ok(new SolarSystemGenerationResponse
            {
                Success = true,
                SystemJson = systemJson,
                ImageBase64 = imageBase64,
                ImageUrl = imageUrl,
                RagContextUsed = ragContext.Any(),
                RagSourceCount = ragContext.Count,
                GenerationRequestId = generationRequest.Id
            });
        }
        catch (UnauthorizedAccessException)
        {
            return Unauthorized();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate solar system");
            return StatusCode(500, new SolarSystemGenerationResponse
            {
                Success = false,
                Error = ex.Message
            });
        }
    }

    /// <summary>
    /// Generate a vehicle with stats using RAG-enhanced AI.
    /// Retrieves relevant lore from game system manuals to ensure lore-accurate generation.
    /// Uses confirmed template field definitions for JSON schema when available.
    /// </summary>
    /// <param name="request">Vehicle generation parameters including game system context.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Generated vehicle data with optional image.</returns>
    [HttpPost("generate/vehicle")]
    [ProducesResponseType(typeof(VehicleGenerationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<VehicleGenerationResponse>> GenerateVehicle(
        [FromBody] VehicleGenerationRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var userId = GetCurrentUserId();
            
            _logger.LogInformation(
                "Generating vehicle: Type={Type}, Class={Class}, GameSystemId={GameSystemId}", 
                request.Type, request.Class, request.GameSystemId);

            // Check for confirmed template to use its field definitions
            var template = await GetConfirmedTemplateAsync(
                request.GameSystemId,
                userId,
                "vehicle",
                cancellationToken);

            string exampleJson;
            string fieldDescriptions;

            if (template != null)
            {
                var fields = template.GetFieldDefinitions();
                var statsJson = BuildExampleJsonFromTemplate(fields);
                // Wrap template fields inside the standard structure
                exampleJson = $@"{{""name"":""<Vehicle Name>"",""description"":""<2-3 sentence description>"",""stats"":{statsJson}}}";
                fieldDescriptions = $@"- name: A unique vehicle designation/name fitting the setting
- description: A 2-3 sentence description of capabilities
- stats: An object containing the following template fields:
{BuildFieldDescriptions(fields)}";
                _logger.LogDebug("Using template {TemplateId} for vehicle generation", template.Id);
            }
            else
            {
                exampleJson = @"{""name"":""Phantom-X7"",""description"":""A stealth interceptor..."",""stats"":{""SPEED"":95,""ARMOR"":40,""CARGO"":20}}";
                fieldDescriptions = @"- name: A unique vehicle designation/name fitting the setting
- description: A 2-3 sentence description of capabilities
- stats: An object with SPEED (1-100), ARMOR (1-100), CARGO (1-100)";
            }

            // Build context for RAG query
            var additionalContext = $"Vehicle type: {request.Type}, Class: {request.Class}, Engine: {request.Engine}";

            // RAG-enhanced system prompt
            var systemPrompt = @"You are a vehicle generator for a tabletop RPG.
Use the provided lore context from the game manuals to create a vehicle that fits the game's setting and technology level.
The vehicle must be consistent with the game system's lore, factions, and available technology.
Respond only with valid JSON.";

            // RAG user query
            var userQuery = $@"Based on the game system lore provided, generate a vehicle with these parameters:
Type: {request.Type}
Class: {request.Class}
Engine: {request.Engine}

Generate a JSON object with the following fields:
{fieldDescriptions}

The vehicle should reflect the game world's technology and design aesthetics from the lore.

Example format:
{exampleJson}";

            // Fallback prompts
            var fallbackPrompt = $@"Create a futuristic vehicle:
Type: {request.Type}
Class: {request.Class}
Engine: {request.Engine}

Respond with a JSON object containing the following fields:
{fieldDescriptions}

Example format:
{exampleJson}";

            var fallbackSystemPrompt = "You are a sci-fi vehicle designer. Respond only with valid JSON.";

            // Generate with RAG
            var (vehicleJson, ragContext, fullPrompt) = await GenerateWithRagAsync(
                request.GameSystemId,
                userId,
                entityType: "vehicle",
                additionalContext: additionalContext,
                systemPrompt: systemPrompt,
                userQuery: userQuery,
                fallbackPrompt: fallbackPrompt,
                fallbackSystemPrompt: fallbackSystemPrompt,
                cancellationToken);

            // Conditionally generate image based on request parameter
            string? imageBase64 = null;
            string? imageUrl = null;
            
            if (request.GenerateImage)
            {
                // Optionally enhance image prompt with style context from RAG
                string imagePromptContext = "";
                
                if (request.GameSystemId.HasValue)
                {
                    var styleContext = await _ragContextProvider.GetStyleContextAsync(
                        request.GameSystemId.Value,
                        userId,
                        "vehicle",
                        cancellationToken);
                    
                    if (styleContext.Any())
                    {
                        imagePromptContext = $" Art style based on: {string.Join(" ", styleContext.Take(2).Select(c => c.Content?.Substring(0, Math.Min(200, c.Content.Length)) ?? ""))}";
                    }
                }

                var imagePrompt = $"Stunning sci-fi {request.Type} design, {request.Class} class, powered by {request.Engine} engine. Sleek futuristic vehicle, detailed mechanical components, cinematic lighting, concept art style, black space background, 8k resolution.{imagePromptContext}";
                var imageResult = await _aiService.GenerateImageAsync(imagePrompt, cancellationToken: cancellationToken);
                imageBase64 = imageResult.ImageBase64;
                imageUrl = imageResult.ImageUrl;
            }

            // Create and persist generation tracking records
            var generationRequest = CreateGenerationRequest(userId, "vehicle", fullPrompt);
            generationRequest.Complete();

            var generationResult = CreateGenerationResult(
                generationRequest.Id,
                vehicleJson,
                ragContext.Any(),
                ragContext.Count,
                imageBase64 != null || imageUrl != null);

            await PersistGenerationRequestWithResultAsync(generationRequest, generationResult, cancellationToken);

            _logger.LogInformation(
                "Vehicle generation completed. GenerationRequestId: {GenerationRequestId}",
                generationRequest.Id);

            return Ok(new VehicleGenerationResponse
            {
                Success = true,
                VehicleJson = vehicleJson,
                ImageBase64 = imageBase64,
                ImageUrl = imageUrl,
                RagContextUsed = ragContext.Any(),
                RagSourceCount = ragContext.Count,
                GenerationRequestId = generationRequest.Id
            });
        }
        catch (UnauthorizedAccessException)
        {
            return Unauthorized();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate vehicle");
            return StatusCode(500, new VehicleGenerationResponse
            {
                Success = false,
                Error = ex.Message
            });
        }
    }

    /// <summary>
    /// Generate an NPC (Non-Player Character) with personality and background using RAG-enhanced AI.
    /// Retrieves relevant lore from game system manuals to ensure lore-accurate generation.
    /// Uses confirmed template field definitions for JSON schema when available.
    /// </summary>
    /// <param name="request">NPC generation parameters including game system context.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Generated NPC data with optional image.</returns>
    [HttpPost("generate/npc")]
    [ProducesResponseType(typeof(NpcGenerationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<NpcGenerationResponse>> GenerateNpc(
        [FromBody] NpcGenerationRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var userId = GetCurrentUserId();
            
            _logger.LogInformation(
                "Generating NPC: Species={Species}, Occupation={Occupation}, GameSystemId={GameSystemId}", 
                request.Species, request.Occupation, request.GameSystemId);

            // Check for confirmed template to use its field definitions
            var template = await GetConfirmedTemplateAsync(
                request.GameSystemId,
                userId,
                "npc",
                cancellationToken);

            string exampleJson;
            string fieldDescriptions;

            if (template != null)
            {
                var fields = template.GetFieldDefinitions();
                var statsJson = BuildExampleJsonFromTemplate(fields);
                // Wrap template fields inside the standard structure
                exampleJson = $@"{{""name"":""<NPC Name>"",""description"":""<2-3 sentence background>"",""stats"":{statsJson}}}";
                fieldDescriptions = $@"- name: A unique sci-fi name fitting their species and the setting's naming conventions
- description: A 2-3 sentence backstory explaining who they are and their motivations
- stats: An object containing the following template fields:
{BuildFieldDescriptions(fields)}";
                _logger.LogDebug("Using template {TemplateId} for NPC generation", template.Id);
            }
            else
            {
                exampleJson = @"{""name"":""Vex Morrow"",""description"":""Former corporate spy turned information broker..."",""stats"":{""occupation"":""Information Broker"",""personality"":""Cunning but fair"",""CHA"":85,""INT"":75,""WIS"":60}}";
                fieldDescriptions = @"- name: A unique sci-fi name fitting their species and the setting's naming conventions
- description: A 2-3 sentence backstory explaining who they are and their motivations
- stats: An object with occupation, personality, and attributes (CHA, INT, WIS from 1-100)";
            }

            // Build context for RAG query
            var additionalContext = $"Species: {request.Species}, Occupation: {request.Occupation}, Personality: {request.Personality}, Setting: {request.Setting}";

            // RAG-enhanced system prompt
            var systemPrompt = @"You are an NPC generator for a tabletop RPG.
Use the provided lore context from the game manuals to create an NPC that fits the game's setting.
The NPC must be consistent with the game system's lore, factions, species, and social structures.
Respond only with valid JSON.";

            // RAG user query
            var userQuery = $@"Based on the game system lore provided, generate an NPC with these parameters:
Species: {request.Species}
Occupation: {request.Occupation}
Personality: {request.Personality}
Setting: {request.Setting}

Generate a JSON object with the following fields:
{fieldDescriptions}

The NPC should reflect the game world's factions, cultures, and social dynamics from the lore.

Example format:
{exampleJson}";

            // Fallback prompts
            var fallbackPrompt = $@"Create a sci-fi NPC (non-player character) for a tabletop RPG:
Species: {request.Species}
Occupation: {request.Occupation}
Personality: {request.Personality}
Setting: {request.Setting}

Respond with a JSON object containing the following fields:
{fieldDescriptions}

Example format:
{exampleJson}";

            var fallbackSystemPrompt = "You are a sci-fi RPG character creator. Create interesting NPCs with depth. Respond only with valid JSON.";

            // Generate with RAG
            var (npcJson, ragContext, fullPrompt) = await GenerateWithRagAsync(
                request.GameSystemId,
                userId,
                entityType: "npc",
                additionalContext: additionalContext,
                systemPrompt: systemPrompt,
                userQuery: userQuery,
                fallbackPrompt: fallbackPrompt,
                fallbackSystemPrompt: fallbackSystemPrompt,
                cancellationToken);

            // Conditionally generate image based on request parameter
            string? imageBase64 = null;
            string? imageUrl = null;
            
            if (request.GenerateImage)
            {
                // Optionally enhance image prompt with style context from RAG
                string imagePromptContext = "";
                
                if (request.GameSystemId.HasValue)
                {
                    var styleContext = await _ragContextProvider.GetStyleContextAsync(
                        request.GameSystemId.Value,
                        userId,
                        "npc",
                        cancellationToken);
                    
                    if (styleContext.Any())
                    {
                        imagePromptContext = $" Art style based on: {string.Join(" ", styleContext.Take(2).Select(c => c.Content?.Substring(0, Math.Min(200, c.Content.Length)) ?? ""))}";
                    }
                }

                var imagePrompt = $"High-quality futuristic sci-fi portrait of a {request.Species} {request.Occupation}, {request.Personality} expression. Cinematic lighting, cyberpunk aesthetic, detailed face, professional concept art, neutral background, 8k resolution.{imagePromptContext}";
                var imageResult = await _aiService.GenerateImageAsync(imagePrompt, cancellationToken: cancellationToken);
                imageBase64 = imageResult.ImageBase64;
                imageUrl = imageResult.ImageUrl;
            }

            // Create and persist generation tracking records
            var generationRequest = CreateGenerationRequest(userId, "npc", fullPrompt);
            generationRequest.Complete();

            var generationResult = CreateGenerationResult(
                generationRequest.Id,
                npcJson,
                ragContext.Any(),
                ragContext.Count,
                imageBase64 != null || imageUrl != null);

            await PersistGenerationRequestWithResultAsync(generationRequest, generationResult, cancellationToken);

            _logger.LogInformation(
                "NPC generation completed. GenerationRequestId: {GenerationRequestId}",
                generationRequest.Id);

            return Ok(new NpcGenerationResponse
            {
                Success = true,
                NpcJson = npcJson,
                ImageBase64 = imageBase64,
                ImageUrl = imageUrl,
                RagContextUsed = ragContext.Any(),
                RagSourceCount = ragContext.Count,
                GenerationRequestId = generationRequest.Id
            });
        }
        catch (UnauthorizedAccessException)
        {
            return Unauthorized();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate NPC");
            return StatusCode(500, new NpcGenerationResponse
            {
                Success = false,
                Error = ex.Message
            });
        }
    }

    /// <summary>
    /// Generate an enemy/hostile creature with combat stats using RAG-enhanced AI.
    /// Retrieves relevant lore from game system manuals to ensure lore-accurate generation.
    /// </summary>
    /// <param name="request">Enemy generation parameters including game system context.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Generated enemy data with optional image.</returns>
    [HttpPost("generate/enemy")]
    [ProducesResponseType(typeof(EnemyGenerationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<EnemyGenerationResponse>> GenerateEnemy(
        [FromBody] EnemyGenerationRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var userId = GetCurrentUserId();
            
            _logger.LogInformation(
                "Generating enemy: Species={Species}, ThreatLevel={ThreatLevel}, GameSystemId={GameSystemId}", 
                request.Species, request.ThreatLevel, request.GameSystemId);

            // Check for confirmed template to use its field definitions
            var template = await GetConfirmedTemplateAsync(
                request.GameSystemId,
                userId,
                "enemy",
                cancellationToken);

            string exampleJson;
            string fieldDescriptions;

            if (template != null)
            {
                var fields = template.GetFieldDefinitions();
                var statsJson = BuildExampleJsonFromTemplate(fields);
                // Wrap template fields inside the standard structure
                exampleJson = $@"{{""name"":""<Enemy Name>"",""description"":""<2-3 sentence description>"",""stats"":{statsJson}}}";
                fieldDescriptions = $@"- name: A threatening designation or creature name fitting the setting
- description: A 2-3 sentence description of the creature and its abilities
- stats: An object containing the following template fields:
{BuildFieldDescriptions(fields)}";
                _logger.LogDebug("Using template {TemplateId} for enemy generation", template.Id);
            }
            else
            {
                exampleJson = @"{""name"":""Void Stalker"",""description"":""An alien predator with cloaking abilities..."",""stats"":{""species"":""Alien Predator"",""threatLevel"":""dangerous"",""abilities"":""Cloaking field, venomous claws"",""weakness"":""Sensitive to bright light"",""HP"":150,""ATK"":75,""DEF"":40,""SPD"":90}}";
                fieldDescriptions = @"- name: A threatening designation or creature name fitting the setting
- description: A 2-3 sentence description of the creature and its abilities
- stats: An object with species, threatLevel, abilities, weakness, HP (50-500), ATK (1-100), DEF (1-100), SPD (1-100)";
            }

            // Build context for RAG query
            var additionalContext = $"Species type: {request.Species}, Threat level: {request.ThreatLevel}, Behavior: {request.Behavior}, Environment: {request.Environment}";

            // RAG-enhanced system prompt
            var systemPrompt = @"You are an enemy/creature generator for a tabletop RPG.
Use the provided lore context from the game manuals to create an enemy that fits the game's setting.
The enemy must be consistent with the game system's lore, bestiary, and combat mechanics.
Respond only with valid JSON.";

            // RAG user query
            var userQuery = $@"Based on the game system lore provided, generate a hostile creature/enemy with these parameters:
Species Type: {request.Species}
Threat Level: {request.ThreatLevel}
Behavior Pattern: {request.Behavior}
Environment: {request.Environment}

Generate a JSON object with the following fields:
{fieldDescriptions}

The enemy should reflect the game world's creatures, factions, and known threats from the lore.

Example format:
{exampleJson}";

            // Fallback prompts
            var fallbackPrompt = $@"Create a hostile sci-fi creature/enemy for a tabletop RPG:
Species Type: {request.Species}
Threat Level: {request.ThreatLevel}
Behavior Pattern: {request.Behavior}
Environment: {request.Environment}

Respond with a JSON object containing the following fields:
{fieldDescriptions}

Example format:
{exampleJson}";

            var fallbackSystemPrompt = "You are a sci-fi monster designer. Create terrifying but balanced enemies. Respond only with valid JSON.";

            // Generate with RAG
            var (enemyJson, ragContext, fullPrompt) = await GenerateWithRagAsync(
                request.GameSystemId,
                userId,
                entityType: "enemy",
                additionalContext: additionalContext,
                systemPrompt: systemPrompt,
                userQuery: userQuery,
                fallbackPrompt: fallbackPrompt,
                fallbackSystemPrompt: fallbackSystemPrompt,
                cancellationToken);

            // Conditionally generate image based on request parameter
            string? imageBase64 = null;
            string? imageUrl = null;
            
            if (request.GenerateImage)
            {
                // Optionally enhance image prompt with style context from RAG
                string imagePromptContext = "";
                
                if (request.GameSystemId.HasValue)
                {
                    var styleContext = await _ragContextProvider.GetStyleContextAsync(
                        request.GameSystemId.Value,
                        userId,
                        "enemy",
                        cancellationToken);
                    
                    if (styleContext.Any())
                    {
                        imagePromptContext = $" Art style based on: {string.Join(" ", styleContext.Take(2).Select(c => c.Content?.Substring(0, Math.Min(200, c.Content.Length)) ?? ""))}";
                    }
                }

                var imagePrompt = $"Terrifying sci-fi creature concept art, {request.Species}, {request.Behavior} posture, menacing, dark atmosphere, highly detailed, horror sci-fi aesthetic, professional illustration, dramatic lighting, 8k resolution.{imagePromptContext}";
                var imageResult = await _aiService.GenerateImageAsync(imagePrompt, cancellationToken: cancellationToken);
                imageBase64 = imageResult.ImageBase64;
                imageUrl = imageResult.ImageUrl;
            }

            // Create and persist generation tracking records
            var generationRequest = CreateGenerationRequest(userId, "enemy", fullPrompt);
            generationRequest.Complete();

            var generationResult = CreateGenerationResult(
                generationRequest.Id,
                enemyJson,
                ragContext.Any(),
                ragContext.Count,
                imageBase64 != null || imageUrl != null);

            await PersistGenerationRequestWithResultAsync(generationRequest, generationResult, cancellationToken);

            _logger.LogInformation(
                "Enemy generation completed. GenerationRequestId: {GenerationRequestId}",
                generationRequest.Id);

            return Ok(new EnemyGenerationResponse
            {
                Success = true,
                EnemyJson = enemyJson,
                ImageBase64 = imageBase64,
                ImageUrl = imageUrl,
                RagContextUsed = ragContext.Any(),
                RagSourceCount = ragContext.Count,
                GenerationRequestId = generationRequest.Id
            });
        }
        catch (UnauthorizedAccessException)
        {
            return Unauthorized();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate enemy");
            return StatusCode(500, new EnemyGenerationResponse
            {
                Success = false,
                Error = ex.Message
            });
        }
    }

    /// <summary>
    /// Generate a mission/quest with objectives and rewards using RAG-enhanced AI.
    /// Retrieves relevant lore from game system manuals to ensure lore-accurate generation.
    /// </summary>
    /// <param name="request">Mission generation parameters including game system context.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Generated mission data with optional image.</returns>
    [HttpPost("generate/mission")]
    [ProducesResponseType(typeof(MissionGenerationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<MissionGenerationResponse>> GenerateMission(
        [FromBody] MissionGenerationRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var userId = GetCurrentUserId();
            
            _logger.LogInformation(
                "Generating mission: Type={MissionType}, Difficulty={Difficulty}, GameSystemId={GameSystemId}", 
                request.MissionType, request.Difficulty, request.GameSystemId);

            // Check for confirmed template to use its field definitions
            var template = await GetConfirmedTemplateAsync(
                request.GameSystemId,
                userId,
                "mission",
                cancellationToken);

            string exampleJson;
            string fieldDescriptions;

            if (template != null)
            {
                var fields = template.GetFieldDefinitions();
                var statsJson = BuildExampleJsonFromTemplate(fields);
                // Wrap template fields inside the standard structure
                exampleJson = $@"{{""name"":""<Mission Name>"",""description"":""<2-3 sentence briefing>"",""stats"":{statsJson}}}";
                fieldDescriptions = $@"- name: A code name or operation title (e.g., ""Operation Silent Dawn"") fitting the setting
- description: A 2-3 sentence mission briefing explaining the situation
- stats: An object containing the following template fields:
{BuildFieldDescriptions(fields)}";
                _logger.LogDebug("Using template {TemplateId} for mission generation", template.Id);
            }
            else
            {
                exampleJson = @"{""name"":""Operation Blackout"",""description"":""Corporate forces have seized the research facility..."",""stats"":{""objective"":""Infiltrate the facility and retrieve the data core"",""rewards"":""5000 credits + reputation boost"",""difficulty"":""HARD"",""estimatedDuration"":""3-4 hours""}}";
                fieldDescriptions = @"- name: A code name or operation title (e.g., ""Operation Silent Dawn"") fitting the setting
- description: A 2-3 sentence mission briefing explaining the situation
- stats: An object with objective, rewards, difficulty, and estimatedDuration";
            }

            // Build context for RAG query
            var additionalContext = $"Mission type: {request.MissionType}, Difficulty: {request.Difficulty}, Environment: {request.Environment}, Faction: {request.FactionInvolved}";

            // RAG-enhanced system prompt
            var systemPrompt = @"You are a mission/quest generator for a tabletop RPG.
Use the provided lore context from the game manuals to create a mission that fits the game's setting.
The mission must be consistent with the game system's lore, factions, and narrative themes.
Respond only with valid JSON.";

            // RAG user query
            var userQuery = $@"Based on the game system lore provided, generate a mission with these parameters:
Mission Type: {request.MissionType}
Difficulty: {request.Difficulty}
Environment: {request.Environment}
Faction Involved: {request.FactionInvolved}

Generate a JSON object with the following fields:
{fieldDescriptions}

The mission should reflect the game world's factions, conflicts, and narrative themes from the lore.

Example format:
{exampleJson}";

            // Fallback prompts
            var fallbackPrompt = $@"Create a sci-fi RPG mission/quest:
Mission Type: {request.MissionType}
Difficulty: {request.Difficulty}
Environment: {request.Environment}
Faction Involved: {request.FactionInvolved}

Respond with a JSON object containing the following fields:
{fieldDescriptions}

Example format:
{exampleJson}";

            var fallbackSystemPrompt = "You are a sci-fi mission designer. Create engaging quests with clear objectives. Respond only with valid JSON.";

            // Generate with RAG
            var (missionJson, ragContext, fullPrompt) = await GenerateWithRagAsync(
                request.GameSystemId,
                userId,
                entityType: "mission",
                additionalContext: additionalContext,
                systemPrompt: systemPrompt,
                userQuery: userQuery,
                fallbackPrompt: fallbackPrompt,
                fallbackSystemPrompt: fallbackSystemPrompt,
                cancellationToken);

            // Conditionally generate image based on request parameter
            string? imageBase64 = null;
            string? imageUrl = null;
            
            if (request.GenerateImage)
            {
                // Optionally enhance image prompt with style context from RAG
                string imagePromptContext = "";
                
                if (request.GameSystemId.HasValue)
                {
                    var styleContext = await _ragContextProvider.GetStyleContextAsync(
                        request.GameSystemId.Value,
                        userId,
                        "mission",
                        cancellationToken);
                    
                    if (styleContext.Any())
                    {
                        imagePromptContext = $" Art style based on: {string.Join(" ", styleContext.Take(2).Select(c => c.Content?.Substring(0, Math.Min(200, c.Content.Length)) ?? ""))}";
                    }
                }

                var imagePrompt = $"Cinematic sci-fi scene depicting a {request.MissionType} mission in a {request.Environment}. Dramatic atmosphere, {request.Difficulty} difficulty feel, tactical environment, concept art style, moody lighting, 8k resolution.{imagePromptContext}";
                var imageResult = await _aiService.GenerateImageAsync(imagePrompt, cancellationToken: cancellationToken);
                imageBase64 = imageResult.ImageBase64;
                imageUrl = imageResult.ImageUrl;
            }

            // Create and persist generation tracking records
            var generationRequest = CreateGenerationRequest(userId, "mission", fullPrompt);
            generationRequest.Complete();

            var generationResult = CreateGenerationResult(
                generationRequest.Id,
                missionJson,
                ragContext.Any(),
                ragContext.Count,
                imageBase64 != null || imageUrl != null);

            await PersistGenerationRequestWithResultAsync(generationRequest, generationResult, cancellationToken);

            _logger.LogInformation(
                "Mission generation completed. GenerationRequestId: {GenerationRequestId}",
                generationRequest.Id);

            return Ok(new MissionGenerationResponse
            {
                Success = true,
                MissionJson = missionJson,
                ImageBase64 = imageBase64,
                ImageUrl = imageUrl,
                RagContextUsed = ragContext.Any(),
                RagSourceCount = ragContext.Count,
                GenerationRequestId = generationRequest.Id
            });
        }
        catch (UnauthorizedAccessException)
        {
            return Unauthorized();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate mission");
            return StatusCode(500, new MissionGenerationResponse
            {
                Success = false,
                Error = ex.Message
            });
        }
    }

    /// <summary>
    /// Generate a combat encounter with participants and environment using RAG-enhanced AI.
    /// Retrieves relevant lore from game system manuals to ensure lore-accurate generation.
    /// </summary>
    /// <param name="request">Encounter generation parameters including game system context.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Generated encounter data with optional image.</returns>
    [HttpPost("generate/encounter")]
    [ProducesResponseType(typeof(EncounterGenerationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<EncounterGenerationResponse>> GenerateEncounter(
        [FromBody] EncounterGenerationRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var userId = GetCurrentUserId();
            
            _logger.LogInformation(
                "Generating encounter: Type={EncounterType}, Difficulty={Difficulty}, GameSystemId={GameSystemId}", 
                request.EncounterType, request.Difficulty, request.GameSystemId);

            // Check for confirmed template to use its field definitions
            var template = await GetConfirmedTemplateAsync(
                request.GameSystemId,
                userId,
                "encounter",
                cancellationToken);

            string exampleJson;
            string fieldDescriptions;

            if (template != null)
            {
                var fields = template.GetFieldDefinitions();
                var statsJson = BuildExampleJsonFromTemplate(fields);
                // Wrap template fields inside the standard structure
                exampleJson = $@"{{""name"":""<Encounter Name>"",""description"":""<2-3 sentence description>"",""stats"":{statsJson}}}";
                fieldDescriptions = $@"- name: An evocative encounter name (e.g., ""Ambush at Sector 7"") fitting the setting
- description: A 2-3 sentence description of the situation and how the encounter begins
- stats: An object containing the following template fields:
{BuildFieldDescriptions(fields)}";
                _logger.LogDebug("Using template {TemplateId} for encounter generation", template.Id);
            }
            else
            {
                exampleJson = @"{""name"":""Cargo Bay Showdown"",""description"":""The party enters the cargo bay to find hostile forces..."",""stats"":{""environment"":""Large open space with shipping containers providing cover"",""participants"":[""Security Droid x2"",""Corporate Enforcer""],""difficulty"":""MEDIUM"",""loot"":""Prototype weapon, 1200 credits""}}";
                fieldDescriptions = @"- name: An evocative encounter name (e.g., ""Ambush at Sector 7"") fitting the setting
- description: A 2-3 sentence description of the situation and how the encounter begins
- stats: An object with environment, participants array, difficulty, and loot";
            }

            // Build context for RAG query
            var additionalContext = $"Encounter type: {request.EncounterType}, Difficulty: {request.Difficulty}, Environment: {request.Environment}, Enemy count: {request.EnemyCount}";

            // RAG-enhanced system prompt
            var systemPrompt = @"You are a combat encounter generator for a tabletop RPG.
Use the provided lore context from the game manuals to create an encounter that fits the game's setting.
The encounter must be consistent with the game system's lore, combat rules, and enemy types.
Respond only with valid JSON.";

            // RAG user query
            var userQuery = $@"Based on the game system lore provided, generate a combat encounter with these parameters:
Encounter Type: {request.EncounterType}
Difficulty: {request.Difficulty}
Environment: {request.Environment}
Enemy Count: {request.EnemyCount}

Generate a JSON object with the following fields:
{fieldDescriptions}

The encounter should reflect the game world's enemies, factions, and combat scenarios from the lore.

Example format:
{exampleJson}";

            // Fallback prompts
            var fallbackPrompt = $@"Create a sci-fi RPG combat/tactical encounter:
Encounter Type: {request.EncounterType}
Difficulty: {request.Difficulty}
Environment: {request.Environment}
Enemy Count: {request.EnemyCount}

Respond with a JSON object containing the following fields:
{fieldDescriptions}

Example format:
{exampleJson}";

            var fallbackSystemPrompt = "You are a sci-fi encounter designer. Create tactical and exciting combat scenarios. Respond only with valid JSON.";

            // Generate with RAG
            var (encounterJson, ragContext, fullPrompt) = await GenerateWithRagAsync(
                request.GameSystemId,
                userId,
                entityType: "encounter",
                additionalContext: additionalContext,
                systemPrompt: systemPrompt,
                userQuery: userQuery,
                fallbackPrompt: fallbackPrompt,
                fallbackSystemPrompt: fallbackSystemPrompt,
                cancellationToken);

            // Conditionally generate image based on request parameter
            string? imageBase64 = null;
            string? imageUrl = null;
            
            if (request.GenerateImage)
            {
                // Optionally enhance image prompt with style context from RAG
                string imagePromptContext = "";
                
                if (request.GameSystemId.HasValue)
                {
                    var styleContext = await _ragContextProvider.GetStyleContextAsync(
                        request.GameSystemId.Value,
                        userId,
                        "encounter",
                        cancellationToken);
                    
                    if (styleContext.Any())
                    {
                        imagePromptContext = $" Art style based on: {string.Join(" ", styleContext.Take(2).Select(c => c.Content?.Substring(0, Math.Min(200, c.Content.Length)) ?? ""))}";
                    }
                }

                var imagePrompt = $"Intense sci-fi {request.EncounterType} encounter in a {request.Environment} environment. {request.EnemyCount} enemies, tactical combat scene, dramatic lighting, action-packed atmosphere, concept art style, 8k resolution.{imagePromptContext}";
                var imageResult = await _aiService.GenerateImageAsync(imagePrompt, cancellationToken: cancellationToken);
                imageBase64 = imageResult.ImageBase64;
                imageUrl = imageResult.ImageUrl;
            }

            // Create and persist generation tracking records
            var generationRequest = CreateGenerationRequest(userId, "encounter", fullPrompt);
            generationRequest.Complete();

            var generationResult = CreateGenerationResult(
                generationRequest.Id,
                encounterJson,
                ragContext.Any(),
                ragContext.Count,
                imageBase64 != null || imageUrl != null);

            await PersistGenerationRequestWithResultAsync(generationRequest, generationResult, cancellationToken);

            _logger.LogInformation(
                "Encounter generation completed. GenerationRequestId: {GenerationRequestId}",
                generationRequest.Id);

            return Ok(new EncounterGenerationResponse
            {
                Success = true,
                EncounterJson = encounterJson,
                ImageBase64 = imageBase64,
                ImageUrl = imageUrl,
                RagContextUsed = ragContext.Any(),
                RagSourceCount = ragContext.Count,
                GenerationRequestId = generationRequest.Id
            });
        }
        catch (UnauthorizedAccessException)
        {
            return Unauthorized();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate encounter");
            return StatusCode(500, new EncounterGenerationResponse
            {
                Success = false,
                Error = ex.Message
            });
        }
    }

    /// <summary>
    /// Test the Genkit AI service connection
    /// </summary>
    [HttpPost("test")]
    [ProducesResponseType(typeof(TestAiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<TestAiResponse>> TestGenkit(
        [FromBody] TestAiRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Testing Genkit AI service with prompt: {Prompt}", request.Prompt);

            var result = await _aiService.GenerateTextAsync(
                request.Prompt,
                request.SystemPrompt,
                request.Temperature ?? 0.7f,
                request.MaxTokens ?? 256,
                cancellationToken);

            return Ok(new TestAiResponse
            {
                Success = true,
                Text = result.Text,
                Usage = result.Usage != null ? new TokenUsageResponse
                {
                    PromptTokens = result.Usage.PromptTokens,
                    CompletionTokens = result.Usage.CompletionTokens,
                    TotalTokens = result.Usage.TotalTokens
                } : null
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to test Genkit AI service");
            return StatusCode(500, new TestAiResponse
            {
                Success = false,
                Error = ex.Message
            });
        }
    }

    /// <summary>
    /// Check if the Genkit AI service is healthy
    /// </summary>
    [HttpGet("health")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(HealthResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<HealthResponse>> CheckHealth(CancellationToken cancellationToken)
    {
        var isHealthy = await _aiService.IsHealthyAsync(cancellationToken);
        
        return Ok(new HealthResponse
        {
            IsHealthy = isHealthy,
            Service = "Genkit AI",
            Timestamp = DateTime.UtcNow
        });
    }
}

// Request/Response DTOs

/// <summary>
/// Request parameters for character generation with RAG support.
/// </summary>
public record CharacterGenerationRequest
{
    /// <summary>
    /// Optional game system ID for RAG-enhanced generation using manual lore.
    /// If not provided, basic generation will be used without lore context.
    /// </summary>
    public Guid? GameSystemId { get; init; }
    
    /// <summary>
    /// Character species/race (e.g., "human", "android", "alien").
    /// </summary>
    public string Species { get; init; } = "human";
    
    /// <summary>
    /// Character role/class (e.g., "operative", "hacker", "medic").
    /// </summary>
    public string Role { get; init; } = "operative";
    
    /// <summary>
    /// Character morphology/body type (e.g., "MASCULINE", "FEMININE", "NEUTRAL").
    /// </summary>
    public string Morphology { get; init; } = "NEUTRAL";
    
    /// <summary>
    /// Character style/attire description.
    /// </summary>
    public string Attire { get; init; } = "Techwear";
    
    /// <summary>
    /// Whether to generate an AI image for this entity. Defaults to true.
    /// </summary>
    public bool GenerateImage { get; init; } = true;
}

/// <summary>
/// Response from character generation including RAG metadata.
/// </summary>
public record CharacterGenerationResponse
{
    /// <summary>
    /// Whether generation was successful.
    /// </summary>
    public bool Success { get; init; }
    
    /// <summary>
    /// Generated character data as JSON string.
    /// </summary>
    public string? CharacterJson { get; init; }
    
    /// <summary>
    /// Generated image as base64 string (if requested).
    /// </summary>
    public string? ImageBase64 { get; init; }
    
    /// <summary>
    /// Generated image URL (if available from provider).
    /// </summary>
    public string? ImageUrl { get; init; }
    
    /// <summary>
    /// Error message if generation failed.
    /// </summary>
    public string? Error { get; init; }
    
    /// <summary>
    /// Whether RAG context from game manuals was used for generation.
    /// </summary>
    public bool RagContextUsed { get; init; }
    
    /// <summary>
    /// Number of RAG context chunks used (0 if none).
    /// </summary>
    public int RagSourceCount { get; init; }

    /// <summary>
    /// Unique identifier for this generation request. 
    /// Use this when saving the entity to link it to its generation history.
    /// </summary>
    public Guid? GenerationRequestId { get; init; }
}

/// <summary>
/// Request parameters for solar system generation with RAG support.
/// </summary>
public record SolarSystemGenerationRequest
{
    /// <summary>
    /// Optional game system ID for RAG-enhanced generation using manual lore.
    /// </summary>
    public Guid? GameSystemId { get; init; }
    
    /// <summary>
    /// Star spectral class (e.g., "G", "M", "K", "O").
    /// </summary>
    public string SpectralClass { get; init; } = "G";
    
    /// <summary>
    /// Number of planets in the system.
    /// </summary>
    public int PlanetCount { get; init; } = 8;
    
    /// <summary>
    /// Whether to generate an AI image for this entity. Defaults to true.
    /// </summary>
    public bool GenerateImage { get; init; } = true;
}

/// <summary>
/// Response from solar system generation including RAG metadata.
/// </summary>
public record SolarSystemGenerationResponse
{
    public bool Success { get; init; }
    public string? SystemJson { get; init; }
    public string? ImageBase64 { get; init; }
    public string? ImageUrl { get; init; }
    public string? Error { get; init; }
    
    /// <summary>
    /// Whether RAG context from game manuals was used for generation.
    /// </summary>
    public bool RagContextUsed { get; init; }
    
    /// <summary>
    /// Number of RAG context chunks used (0 if none).
    /// </summary>
    public int RagSourceCount { get; init; }

    /// <summary>
    /// Unique identifier for this generation request. 
    /// Use this when saving the entity to link it to its generation history.
    /// </summary>
    public Guid? GenerationRequestId { get; init; }
}

/// <summary>
/// Request parameters for vehicle generation with RAG support.
/// </summary>
public record VehicleGenerationRequest
{
    /// <summary>
    /// Optional game system ID for RAG-enhanced generation using manual lore.
    /// </summary>
    public Guid? GameSystemId { get; init; }
    
    /// <summary>
    /// Vehicle type (e.g., "starship", "ground vehicle", "mech").
    /// </summary>
    public string Type { get; init; } = "starship";
    
    /// <summary>
    /// Vehicle class (e.g., "interceptor", "freighter", "battleship").
    /// </summary>
    public string Class { get; init; } = "interceptor";
    
    /// <summary>
    /// Engine type (e.g., "fusion", "ion", "warp").
    /// </summary>
    public string Engine { get; init; } = "fusion";
    
    /// <summary>
    /// Whether to generate an AI image for this entity. Defaults to true.
    /// </summary>
    public bool GenerateImage { get; init; } = true;
}

/// <summary>
/// Response from vehicle generation including RAG metadata.
/// </summary>
public record VehicleGenerationResponse
{
    public bool Success { get; init; }
    public string? VehicleJson { get; init; }
    public string? ImageBase64 { get; init; }
    public string? ImageUrl { get; init; }
    public string? Error { get; init; }
    
    /// <summary>
    /// Whether RAG context from game manuals was used for generation.
    /// </summary>
    public bool RagContextUsed { get; init; }
    
    /// <summary>
    /// Number of RAG context chunks used (0 if none).
    /// </summary>
    public int RagSourceCount { get; init; }

    /// <summary>
    /// Unique identifier for this generation request. 
    /// Use this when saving the entity to link it to its generation history.
    /// </summary>
    public Guid? GenerationRequestId { get; init; }
}

public record TestAiRequest
{
    public string Prompt { get; init; } = "Hello, can you confirm you are working?";
    public string? SystemPrompt { get; init; }
    public float? Temperature { get; init; }
    public int? MaxTokens { get; init; }
}

public record TestAiResponse
{
    public bool Success { get; init; }
    public string? Text { get; init; }
    public string? Error { get; init; }
    public TokenUsageResponse? Usage { get; init; }
}

public record TokenUsageResponse
{
    public int PromptTokens { get; init; }
    public int CompletionTokens { get; init; }
    public int TotalTokens { get; init; }
}

public record HealthResponse
{
    public bool IsHealthy { get; init; }
    public string Service { get; init; } = null!;
    public DateTime Timestamp { get; init; }
}

/// <summary>
/// Request parameters for NPC generation with RAG support.
/// </summary>
public record NpcGenerationRequest
{
    /// <summary>
    /// Optional game system ID for RAG-enhanced generation using manual lore.
    /// </summary>
    public Guid? GameSystemId { get; init; }
    
    /// <summary>
    /// NPC species/race.
    /// </summary>
    public string Species { get; init; } = "human";
    
    /// <summary>
    /// NPC occupation/role.
    /// </summary>
    public string Occupation { get; init; } = "merchant";
    
    /// <summary>
    /// NPC personality traits.
    /// </summary>
    public string Personality { get; init; } = "friendly";
    
    /// <summary>
    /// Setting where NPC is found.
    /// </summary>
    public string Setting { get; init; } = "space-station";
    
    /// <summary>
    /// Whether to generate an AI image for this entity. Defaults to true.
    /// </summary>
    public bool GenerateImage { get; init; } = true;
}

/// <summary>
/// Response from NPC generation including RAG metadata.
/// </summary>
public record NpcGenerationResponse
{
    public bool Success { get; init; }
    public string? NpcJson { get; init; }
    public string? ImageBase64 { get; init; }
    public string? ImageUrl { get; init; }
    public string? Error { get; init; }
    
    /// <summary>
    /// Whether RAG context from game manuals was used for generation.
    /// </summary>
    public bool RagContextUsed { get; init; }
    
    /// <summary>
    /// Number of RAG context chunks used (0 if none).
    /// </summary>
    public int RagSourceCount { get; init; }

    /// <summary>
    /// Unique identifier for this generation request. 
    /// Use this when saving the entity to link it to its generation history.
    /// </summary>
    public Guid? GenerationRequestId { get; init; }
}

/// <summary>
/// Request parameters for enemy generation with RAG support.
/// </summary>
public record EnemyGenerationRequest
{
    /// <summary>
    /// Optional game system ID for RAG-enhanced generation using manual lore.
    /// </summary>
    public Guid? GameSystemId { get; init; }
    
    /// <summary>
    /// Enemy species/type.
    /// </summary>
    public string Species { get; init; } = "alien-beast";
    
    /// <summary>
    /// Threat level (e.g., "low", "moderate", "high", "extreme").
    /// </summary>
    public string ThreatLevel { get; init; } = "moderate";
    
    /// <summary>
    /// Behavior pattern (e.g., "aggressive", "territorial", "pack-hunter").
    /// </summary>
    public string Behavior { get; init; } = "aggressive";
    
    /// <summary>
    /// Environment where enemy is found.
    /// </summary>
    public string Environment { get; init; } = "space-station";
    
    /// <summary>
    /// Whether to generate an AI image for this entity. Defaults to true.
    /// </summary>
    public bool GenerateImage { get; init; } = true;
}

/// <summary>
/// Response from enemy generation including RAG metadata.
/// </summary>
public record EnemyGenerationResponse
{
    public bool Success { get; init; }
    public string? EnemyJson { get; init; }
    public string? ImageBase64 { get; init; }
    public string? ImageUrl { get; init; }
    public string? Error { get; init; }
    
    /// <summary>
    /// Whether RAG context from game manuals was used for generation.
    /// </summary>
    public bool RagContextUsed { get; init; }
    
    /// <summary>
    /// Number of RAG context chunks used (0 if none).
    /// </summary>
    public int RagSourceCount { get; init; }

    /// <summary>
    /// Unique identifier for this generation request. 
    /// Use this when saving the entity to link it to its generation history.
    /// </summary>
    public Guid? GenerationRequestId { get; init; }
}

/// <summary>
/// Request parameters for mission generation with RAG support.
/// </summary>
public record MissionGenerationRequest
{
    /// <summary>
    /// Optional game system ID for RAG-enhanced generation using manual lore.
    /// </summary>
    public Guid? GameSystemId { get; init; }
    
    /// <summary>
    /// Mission type (e.g., "extraction", "assassination", "escort").
    /// </summary>
    public string MissionType { get; init; } = "extraction";
    
    /// <summary>
    /// Mission difficulty.
    /// </summary>
    public string Difficulty { get; init; } = "MEDIUM";
    
    /// <summary>
    /// Environment setting for the mission.
    /// </summary>
    public string Environment { get; init; } = "space-station";
    
    /// <summary>
    /// Faction involved in the mission.
    /// </summary>
    public string FactionInvolved { get; init; } = "corporate";
    
    /// <summary>
    /// Whether to generate an AI image for this entity. Defaults to true.
    /// </summary>
    public bool GenerateImage { get; init; } = true;
}

/// <summary>
/// Response from mission generation including RAG metadata.
/// </summary>
public record MissionGenerationResponse
{
    public bool Success { get; init; }
    public string? MissionJson { get; init; }
    public string? ImageBase64 { get; init; }
    public string? ImageUrl { get; init; }
    public string? Error { get; init; }
    
    /// <summary>
    /// Whether RAG context from game manuals was used for generation.
    /// </summary>
    public bool RagContextUsed { get; init; }
    
    /// <summary>
    /// Number of RAG context chunks used (0 if none).
    /// </summary>
    public int RagSourceCount { get; init; }

    /// <summary>
    /// Unique identifier for this generation request. 
    /// Use this when saving the entity to link it to its generation history.
    /// </summary>
    public Guid? GenerationRequestId { get; init; }
}

/// <summary>
/// Request parameters for encounter generation with RAG support.
/// </summary>
public record EncounterGenerationRequest
{
    /// <summary>
    /// Optional game system ID for RAG-enhanced generation using manual lore.
    /// </summary>
    public Guid? GameSystemId { get; init; }
    
    /// <summary>
    /// Encounter type (e.g., "combat", "ambush", "negotiation").
    /// </summary>
    public string EncounterType { get; init; } = "combat";
    
    /// <summary>
    /// Encounter difficulty.
    /// </summary>
    public string Difficulty { get; init; } = "MEDIUM";
    
    /// <summary>
    /// Environment setting for the encounter.
    /// </summary>
    public string Environment { get; init; } = "open-area";
    
    /// <summary>
    /// Number/type of enemies (e.g., "solo", "squad", "horde").
    /// </summary>
    public string EnemyCount { get; init; } = "squad";
    
    /// <summary>
    /// Whether to generate an AI image for this entity. Defaults to true.
    /// </summary>
    public bool GenerateImage { get; init; } = true;
}

/// <summary>
/// Response from encounter generation including RAG metadata.
/// </summary>
public record EncounterGenerationResponse
{
    public bool Success { get; init; }
    public string? EncounterJson { get; init; }
    public string? ImageBase64 { get; init; }
    public string? ImageUrl { get; init; }
    public string? Error { get; init; }
    
    /// <summary>
    /// Whether RAG context from game manuals was used for generation.
    /// </summary>
    public bool RagContextUsed { get; init; }
    
    /// <summary>
    /// Number of RAG context chunks used (0 if none).
    /// </summary>
    public int RagSourceCount { get; init; }

    /// <summary>
    /// Unique identifier for this generation request. 
    /// Use this when saving the entity to link it to its generation history.
    /// </summary>
    public Guid? GenerationRequestId { get; init; }
}
