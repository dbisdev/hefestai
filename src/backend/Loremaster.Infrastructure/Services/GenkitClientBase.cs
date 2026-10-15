using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Loremaster.Application.Common.Interfaces;
using Microsoft.Extensions.Logging;

namespace Loremaster.Infrastructure.Services;

/// <summary>
/// Base class for Genkit microservice HTTP clients.
/// Encapsulates shared HTTP transport concerns: authentication token generation,
/// JSON serialization settings, request dispatch, and error handling.
/// Concrete subclasses only need to provide the typed service name for log messages.
/// </summary>
public abstract class GenkitClientBase
{
    /// <summary>
    /// The typed HttpClient pre-configured with the Genkit base URL.
    /// Exposed as protected to allow subclasses to make non-POST requests (e.g. health checks).
    /// </summary>
    protected readonly HttpClient HttpClient;

    private readonly IServiceTokenGenerator _tokenGenerator;
    private readonly JsonSerializerOptions _jsonOptions;

    /// <summary>
    /// Logger exposed to subclasses for domain-specific diagnostic messages.
    /// </summary>
    protected readonly ILogger Logger;

    /// <summary>
    /// Initializes the Genkit HTTP client base.
    /// </summary>
    /// <param name="httpClient">Named/typed HttpClient pre-configured with the Genkit base URL.</param>
    /// <param name="tokenGenerator">Service-to-service JWT token generator.</param>
    /// <param name="logger">Logger instance provided by the concrete subclass.</param>
    protected GenkitClientBase(
        HttpClient httpClient,
        IServiceTokenGenerator tokenGenerator,
        ILogger logger)
    {
        HttpClient = httpClient;
        _tokenGenerator = tokenGenerator;
        Logger = logger;

        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        };
    }

    /// <summary>
    /// Sends an authenticated POST request to a Genkit endpoint and deserializes the response.
    /// </summary>
    /// <typeparam name="TRequest">Type of the request body.</typeparam>
    /// <typeparam name="TResponse">Type of the expected response body.</typeparam>
    /// <param name="endpoint">Relative path, e.g. "/api/generate".</param>
    /// <param name="request">Request payload to serialize as JSON.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Deserialized response object.</returns>
    /// <exception cref="HttpRequestException">Thrown when the HTTP response is not successful.</exception>
    /// <exception cref="InvalidOperationException">Thrown when the response body cannot be deserialized.</exception>
    protected async Task<TResponse> SendRequestAsync<TRequest, TResponse>(
        string endpoint,
        TRequest request,
        CancellationToken cancellationToken)
    {
        var token = _tokenGenerator.GenerateServiceToken(ServiceScopes.GenkitExecute);

        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, endpoint);
        httpRequest.Headers.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        httpRequest.Content = JsonContent.Create(request, options: _jsonOptions);

        Logger.LogDebug("Sending request to Genkit: {Endpoint}", endpoint);

        var response = await HttpClient.SendAsync(httpRequest, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
            Logger.LogError(
                "Genkit request failed: {StatusCode} - {Content}",
                response.StatusCode,
                errorContent);

            throw new HttpRequestException(
                $"Genkit request failed with status {response.StatusCode}: {errorContent}");
        }

        var result = await response.Content
            .ReadFromJsonAsync<TResponse>(_jsonOptions, cancellationToken);

        if (result == null)
        {
            throw new InvalidOperationException("Failed to deserialize Genkit response");
        }

        return result;
    }

    /// <summary>
    /// Shared DTO for token usage information returned by Genkit endpoints.
    /// </summary>
    protected record TokenUsageDto
    {
        public int PromptTokens { get; init; }
        public int CompletionTokens { get; init; }
        public int TotalTokens { get; init; }
    }

    /// <summary>
    /// Maps a nullable <see cref="TokenUsageDto"/> from the Genkit response to the
    /// application-layer <see cref="Loremaster.Application.Common.Models.TokenUsage"/> model.
    /// Returns <see langword="null"/> when the DTO is absent.
    /// </summary>
    protected static Application.Common.Models.TokenUsage? MapTokenUsage(TokenUsageDto? usage)
    {
        if (usage is null) return null;
        return new Application.Common.Models.TokenUsage(
            usage.PromptTokens, usage.CompletionTokens, usage.TotalTokens);
    }
}
