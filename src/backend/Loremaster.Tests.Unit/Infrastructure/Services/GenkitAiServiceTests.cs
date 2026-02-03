using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Loremaster.Application.Common.Interfaces;
using Loremaster.Infrastructure.Services;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;

namespace Loremaster.Tests.Unit.Infrastructure.Services;

public class GenkitAiServiceTests
{
    private readonly Mock<IServiceTokenGenerator> _tokenGeneratorMock;
    private readonly Mock<ILogger<GenkitAiService>> _loggerMock;
    private readonly Mock<HttpMessageHandler> _httpHandlerMock;
    private readonly HttpClient _httpClient;
    private readonly GenkitAiService _sut;

    public GenkitAiServiceTests()
    {
        _tokenGeneratorMock = new Mock<IServiceTokenGenerator>();
        _loggerMock = new Mock<ILogger<GenkitAiService>>();
        _httpHandlerMock = new Mock<HttpMessageHandler>();
        
        _httpClient = new HttpClient(_httpHandlerMock.Object)
        {
            BaseAddress = new Uri("http://localhost:3001")
        };

        _tokenGeneratorMock
            .Setup(x => x.GenerateServiceToken(It.IsAny<string>()))
            .Returns("test-service-token");

        _sut = new GenkitAiService(_httpClient, _tokenGeneratorMock.Object, _loggerMock.Object);
    }

    #region GenerateTextAsync Tests

    [Fact]
    public async Task GenerateTextAsync_WithValidPrompt_ReturnsGeneratedText()
    {
        // Arrange
        var expectedResponse = new
        {
            text = "Generated response text",
            usage = new { promptTokens = 10, completionTokens = 20, totalTokens = 30 }
        };

        SetupHttpResponse(HttpStatusCode.OK, expectedResponse);

        // Act
        var result = await _sut.GenerateTextAsync("Test prompt");

        // Assert
        result.Should().NotBeNull();
        result.Text.Should().Be("Generated response text");
        result.Usage.Should().NotBeNull();
        result.Usage!.PromptTokens.Should().Be(10);
        result.Usage.CompletionTokens.Should().Be(20);
        result.Usage.TotalTokens.Should().Be(30);
    }

    [Fact]
    public async Task GenerateTextAsync_WithSystemPrompt_SendsCorrectRequest()
    {
        // Arrange
        var expectedResponse = new { text = "Response", usage = (object?)null };
        SetupHttpResponse(HttpStatusCode.OK, expectedResponse);

        // Act
        await _sut.GenerateTextAsync("User prompt", "System prompt", 0.5f, 1024);

        // Assert
        _httpHandlerMock.Protected().Verify(
            "SendAsync",
            Times.Once(),
            ItExpr.Is<HttpRequestMessage>(req =>
                req.Method == HttpMethod.Post &&
                req.RequestUri!.ToString().Contains("/api/generate") &&
                req.Headers.Authorization!.Scheme == "Bearer" &&
                req.Headers.Authorization.Parameter == "test-service-token"),
            ItExpr.IsAny<CancellationToken>());
    }

    [Fact]
    public async Task GenerateTextAsync_WhenServiceReturnsError_ThrowsHttpRequestException()
    {
        // Arrange
        SetupHttpResponse(HttpStatusCode.InternalServerError, new { error = "AI service error" });

        // Act & Assert
        await Assert.ThrowsAsync<HttpRequestException>(() =>
            _sut.GenerateTextAsync("Test prompt"));
    }

    [Fact]
    public async Task GenerateTextAsync_WithoutUsage_ReturnsNullUsage()
    {
        // Arrange
        var expectedResponse = new { text = "Response without usage", usage = (object?)null };
        SetupHttpResponse(HttpStatusCode.OK, expectedResponse);

        // Act
        var result = await _sut.GenerateTextAsync("Test prompt");

        // Assert
        result.Text.Should().Be("Response without usage");
        result.Usage.Should().BeNull();
    }

    #endregion

    #region ChatAsync Tests

    [Fact]
    public async Task ChatAsync_WithValidMessages_ReturnsChatResponse()
    {
        // Arrange
        var messages = new List<ChatMessage>
        {
            new(ChatRole.User, "Hello"),
            new(ChatRole.Assistant, "Hi there!"),
            new(ChatRole.User, "How are you?")
        };

        var expectedResponse = new
        {
            message = "I'm doing well, thank you!",
            usage = new { promptTokens = 15, completionTokens = 10, totalTokens = 25 }
        };

        SetupHttpResponse(HttpStatusCode.OK, expectedResponse);

        // Act
        var result = await _sut.ChatAsync(messages);

        // Assert
        result.Should().NotBeNull();
        result.Message.Should().Be("I'm doing well, thank you!");
        result.Usage.Should().NotBeNull();
    }

    [Fact]
    public async Task ChatAsync_WithContext_SendsContextInRequest()
    {
        // Arrange
        var messages = new List<ChatMessage> { new(ChatRole.User, "Tell me about the world") };
        var context = "This is a fantasy world with dragons";
        var expectedResponse = new { message = "In this world...", usage = (object?)null };

        string? capturedContent = null;
        _httpHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .Callback<HttpRequestMessage, CancellationToken>((req, _) => 
                capturedContent = req.Content?.ReadAsStringAsync().GetAwaiter().GetResult())
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(expectedResponse)
            });

        // Act
        await _sut.ChatAsync(messages, context);

        // Assert
        capturedContent.Should().NotBeNull();
        capturedContent.Should().Contain("fantasy world with dragons");
    }

    [Fact]
    public async Task ChatAsync_ConvertsChatRolesToLowercase()
    {
        // Arrange
        var messages = new List<ChatMessage>
        {
            new(ChatRole.System, "You are helpful"),
            new(ChatRole.User, "Hello")
        };

        string? capturedContent = null;
        _httpHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .Callback<HttpRequestMessage, CancellationToken>((req, _) => 
                capturedContent = req.Content?.ReadAsStringAsync().GetAwaiter().GetResult())
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(new { message = "Hi", usage = (object?)null })
            });

        // Act
        await _sut.ChatAsync(messages);

        // Assert
        capturedContent.Should().Contain("\"role\":\"system\"");
        capturedContent.Should().Contain("\"role\":\"user\"");
    }

    #endregion

    #region SummarizeAsync Tests

    [Fact]
    public async Task SummarizeAsync_WithValidText_ReturnsSummary()
    {
        // Arrange
        var longText = "This is a very long text that needs to be summarized. " +
                       "It contains many details and information that can be condensed.";

        var expectedResponse = new
        {
            summary = "A long text with details that can be condensed.",
            originalLength = 120,
            summaryLength = 45,
            compressionRatio = 0.375,
            usage = new { promptTokens = 30, completionTokens = 15, totalTokens = 45 }
        };

        SetupHttpResponse(HttpStatusCode.OK, expectedResponse);

        // Act
        var result = await _sut.SummarizeAsync(longText);

        // Assert
        result.Should().NotBeNull();
        result.Summary.Should().Be("A long text with details that can be condensed.");
        result.OriginalLength.Should().Be(120);
        result.SummaryLength.Should().Be(45);
        result.CompressionRatio.Should().BeApproximately(0.375, 0.001);
    }

    [Theory]
    [InlineData(SummarizeStyle.Concise, "concise")]
    [InlineData(SummarizeStyle.Detailed, "detailed")]
    [InlineData(SummarizeStyle.BulletPoints, "bullet-points")]
    public async Task SummarizeAsync_WithDifferentStyles_SendsCorrectStyleParameter(
        SummarizeStyle style, string expectedStyleString)
    {
        // Arrange
        string? capturedContent = null;
        _httpHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .Callback<HttpRequestMessage, CancellationToken>((req, _) => 
                capturedContent = req.Content?.ReadAsStringAsync().GetAwaiter().GetResult())
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(new
                {
                    summary = "Summary",
                    originalLength = 100,
                    summaryLength = 20,
                    compressionRatio = 0.2,
                    usage = (object?)null
                })
            });

        // Act
        await _sut.SummarizeAsync("Some text to summarize", style);

        // Assert
        capturedContent.Should().Contain($"\"style\":\"{expectedStyleString}\"");
    }

    [Fact]
    public async Task SummarizeAsync_WithLanguageParameter_SendsLanguageInRequest()
    {
        // Arrange
        string? capturedContent = null;
        _httpHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .Callback<HttpRequestMessage, CancellationToken>((req, _) => 
                capturedContent = req.Content?.ReadAsStringAsync().GetAwaiter().GetResult())
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(new
                {
                    summary = "Resumen",
                    originalLength = 100,
                    summaryLength = 20,
                    compressionRatio = 0.2,
                    usage = (object?)null
                })
            });

        // Act
        await _sut.SummarizeAsync("Texto para resumir", language: "es");

        // Assert
        capturedContent.Should().Contain("\"language\":\"es\"");
    }

    #endregion

    #region IsHealthyAsync Tests

    [Fact]
    public async Task IsHealthyAsync_WhenServiceIsHealthy_ReturnsTrue()
    {
        // Arrange
        _httpHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req => req.RequestUri!.ToString().Contains("/health")),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK));

        // Act
        var result = await _sut.IsHealthyAsync();

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task IsHealthyAsync_WhenServiceIsUnhealthy_ReturnsFalse()
    {
        // Arrange
        _httpHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req => req.RequestUri!.ToString().Contains("/health")),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.ServiceUnavailable));

        // Act
        var result = await _sut.IsHealthyAsync();

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task IsHealthyAsync_WhenExceptionOccurs_ReturnsFalse()
    {
        // Arrange
        _httpHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(new HttpRequestException("Connection refused"));

        // Act
        var result = await _sut.IsHealthyAsync();

        // Assert
        result.Should().BeFalse();
    }

    #endregion

    #region Token Generation Tests

    [Fact]
    public async Task GenerateTextAsync_UsesServiceTokenGenerator()
    {
        // Arrange
        SetupHttpResponse(HttpStatusCode.OK, new { text = "Response", usage = (object?)null });

        // Act
        await _sut.GenerateTextAsync("Test");

        // Assert
        _tokenGeneratorMock.Verify(
            x => x.GenerateServiceToken(ServiceScopes.GenkitExecute),
            Times.Once);
    }

    [Fact]
    public async Task ChatAsync_UsesServiceTokenGenerator()
    {
        // Arrange
        SetupHttpResponse(HttpStatusCode.OK, new { message = "Response", usage = (object?)null });

        // Act
        await _sut.ChatAsync(new[] { new ChatMessage(ChatRole.User, "Hi") });

        // Assert
        _tokenGeneratorMock.Verify(
            x => x.GenerateServiceToken(ServiceScopes.GenkitExecute),
            Times.Once);
    }

    [Fact]
    public async Task SummarizeAsync_UsesServiceTokenGenerator()
    {
        // Arrange
        SetupHttpResponse(HttpStatusCode.OK, new
        {
            summary = "Summary",
            originalLength = 100,
            summaryLength = 20,
            compressionRatio = 0.2,
            usage = (object?)null
        });

        // Act
        await _sut.SummarizeAsync("Text to summarize");

        // Assert
        _tokenGeneratorMock.Verify(
            x => x.GenerateServiceToken(ServiceScopes.GenkitExecute),
            Times.Once);
    }

    #endregion

    #region Error Handling Tests

    [Fact]
    public async Task GenerateTextAsync_WhenUnauthorized_ThrowsHttpRequestException()
    {
        // Arrange
        SetupHttpResponse(HttpStatusCode.Unauthorized, new { error = "Invalid token" });

        // Act & Assert
        var exception = await Assert.ThrowsAsync<HttpRequestException>(() =>
            _sut.GenerateTextAsync("Test"));

        exception.Message.Should().Contain("Unauthorized");
    }

    [Fact]
    public async Task GenerateTextAsync_WhenForbidden_ThrowsHttpRequestException()
    {
        // Arrange
        SetupHttpResponse(HttpStatusCode.Forbidden, new { error = "Insufficient scope" });

        // Act & Assert
        var exception = await Assert.ThrowsAsync<HttpRequestException>(() =>
            _sut.GenerateTextAsync("Test"));

        exception.Message.Should().Contain("Forbidden");
    }

    [Fact]
    public async Task GenerateTextAsync_WhenBadRequest_ThrowsHttpRequestException()
    {
        // Arrange
        SetupHttpResponse(HttpStatusCode.BadRequest, new { error = "Validation error" });

        // Act & Assert
        var exception = await Assert.ThrowsAsync<HttpRequestException>(() =>
            _sut.GenerateTextAsync("Test"));

        exception.Message.Should().Contain("BadRequest");
    }

    #endregion

    #region Helper Methods

    private void SetupHttpResponse(HttpStatusCode statusCode, object responseContent)
    {
        _httpHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(statusCode)
            {
                Content = JsonContent.Create(responseContent)
            });
    }

    #endregion
}
