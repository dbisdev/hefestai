using Loremaster.Domain.ValueObjects;

namespace Loremaster.Tests.Unit.Domain.ValueObjects;

/// <summary>
/// Unit tests for the EntityGenerationResult and EntityImageGenerationResult value objects.
/// Tests creation, validation, and result handling (EPIC 4.5 - Entity Assisted Generation).
/// </summary>
public class EntityGenerationResultTests
{
    #region EntityGenerationResult - Successful Tests

    [Fact]
    public void Successful_WithMinimalData_ShouldCreateSuccessfulResult()
    {
        // Arrange
        var fields = new Dictionary<string, object?>
        {
            ["name"] = "Gandalf",
            ["level"] = 20
        };

        // Act
        var result = EntityGenerationResult.Successful(fields);

        // Assert
        result.Success.Should().BeTrue();
        result.GeneratedFields.Should().HaveCount(2);
        result.ErrorMessage.Should().BeNull();
    }

    [Fact]
    public void Successful_WithAllData_ShouldCreateCompleteResult()
    {
        // Arrange
        var fields = new Dictionary<string, object?> { ["name"] = "Gandalf" };
        var contextChunks = new[] { "Wizard rules from manual", "Character creation guidelines" };

        // Act
        var result = EntityGenerationResult.Successful(
            generatedFields: fields,
            suggestedName: "Gandalf the Grey",
            suggestedDescription: "A wise old wizard",
            contextChunks: contextChunks,
            tokenUsage: new GenerationTokenUsage(100, 50, 150));

        // Assert
        result.Success.Should().BeTrue();
        result.SuggestedName.Should().Be("Gandalf the Grey");
        result.SuggestedDescription.Should().Be("A wise old wizard");
        result.ContextChunks.Should().HaveCount(2);
        result.TokenUsage.Should().NotBeNull();
        result.TokenUsage!.TotalTokens.Should().Be(150);
    }

    [Fact]
    public void Successful_ShouldTrimSuggestedName()
    {
        // Arrange
        var fields = new Dictionary<string, object?>();

        // Act
        var result = EntityGenerationResult.Successful(
            generatedFields: fields,
            suggestedName: "  Gandalf  ");

        // Assert
        result.SuggestedName.Should().Be("Gandalf");
    }

    [Fact]
    public void Successful_ShouldTrimSuggestedDescription()
    {
        // Arrange
        var fields = new Dictionary<string, object?>();

        // Act
        var result = EntityGenerationResult.Successful(
            generatedFields: fields,
            suggestedDescription: "  A wise wizard  ");

        // Assert
        result.SuggestedDescription.Should().Be("A wise wizard");
    }

    #endregion

    #region EntityGenerationResult - Failed Tests

    [Fact]
    public void Failed_ShouldCreateFailedResult()
    {
        // Act
        var result = EntityGenerationResult.Failed("Generation failed due to API error");

        // Assert
        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Be("Generation failed due to API error");
        result.GeneratedFields.Should().BeEmpty();
        result.SuggestedName.Should().BeNull();
        result.SuggestedDescription.Should().BeNull();
        result.ContextChunks.Should().BeEmpty();
        result.TokenUsage.Should().BeNull();
    }

    [Fact]
    public void Failed_ShouldTrimErrorMessage()
    {
        // Act
        var result = EntityGenerationResult.Failed("  Error occurred  ");

        // Assert
        result.ErrorMessage.Should().Be("Error occurred");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Failed_WithEmptyErrorMessage_ShouldThrowArgumentException(string? invalidMessage)
    {
        // Act
        var act = () => EntityGenerationResult.Failed(invalidMessage!);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithParameterName("errorMessage");
    }

    #endregion

    #region EntityGenerationResult - GetFieldValue Tests

    [Fact]
    public void GetFieldValue_WithExistingStringField_ShouldReturnValue()
    {
        // Arrange
        var fields = new Dictionary<string, object?> { ["name"] = "Gandalf" };
        var result = EntityGenerationResult.Successful(fields);

        // Act
        var value = result.GetFieldValue<string>("name");

        // Assert
        value.Should().Be("Gandalf");
    }

    [Fact]
    public void GetFieldValue_WithExistingIntField_ShouldReturnValue()
    {
        // Arrange
        var fields = new Dictionary<string, object?> { ["level"] = 20 };
        var result = EntityGenerationResult.Successful(fields);

        // Act
        var value = result.GetFieldValue<int>("level");

        // Assert
        value.Should().Be(20);
    }

    [Fact]
    public void GetFieldValue_WithNonExistentField_ShouldReturnDefault()
    {
        // Arrange
        var fields = new Dictionary<string, object?>();
        var result = EntityGenerationResult.Successful(fields);

        // Act
        var stringValue = result.GetFieldValue<string>("nonExistent");
        var intValue = result.GetFieldValue<int>("nonExistent");

        // Assert
        stringValue.Should().BeNull();
        intValue.Should().Be(0);
    }

    [Fact]
    public void GetFieldValue_WithConvertibleType_ShouldConvert()
    {
        // Arrange - store as string, retrieve as int
        var fields = new Dictionary<string, object?> { ["level"] = "20" };
        var result = EntityGenerationResult.Successful(fields);

        // Act
        var value = result.GetFieldValue<int>("level");

        // Assert
        value.Should().Be(20);
    }

    [Fact]
    public void GetFieldValue_WithInconvertibleType_ShouldReturnDefault()
    {
        // Arrange
        var fields = new Dictionary<string, object?> { ["name"] = "Gandalf" };
        var result = EntityGenerationResult.Successful(fields);

        // Act
        var value = result.GetFieldValue<int>("name");

        // Assert
        value.Should().Be(0);
    }

    #endregion

    #region EntityGenerationResult - HasField Tests

    [Fact]
    public void HasField_WithExistingField_ShouldReturnTrue()
    {
        // Arrange
        var fields = new Dictionary<string, object?> { ["name"] = "Gandalf" };
        var result = EntityGenerationResult.Successful(fields);

        // Act & Assert
        result.HasField("name").Should().BeTrue();
    }

    [Fact]
    public void HasField_WithNonExistentField_ShouldReturnFalse()
    {
        // Arrange
        var fields = new Dictionary<string, object?>();
        var result = EntityGenerationResult.Successful(fields);

        // Act & Assert
        result.HasField("name").Should().BeFalse();
    }

    [Fact]
    public void HasField_WithNullValueField_ShouldReturnTrue()
    {
        // Arrange - field exists but has null value
        var fields = new Dictionary<string, object?> { ["description"] = null };
        var result = EntityGenerationResult.Successful(fields);

        // Act & Assert
        result.HasField("description").Should().BeTrue();
    }

    #endregion

    #region GenerationTokenUsage Tests

    [Fact]
    public void GenerationTokenUsage_ShouldStoreAllValues()
    {
        // Act
        var usage = new GenerationTokenUsage(100, 50, 150);

        // Assert
        usage.PromptTokens.Should().Be(100);
        usage.CompletionTokens.Should().Be(50);
        usage.TotalTokens.Should().Be(150);
    }

    [Fact]
    public void GenerationTokenUsage_Equality_ShouldWork()
    {
        // Arrange
        var usage1 = new GenerationTokenUsage(100, 50, 150);
        var usage2 = new GenerationTokenUsage(100, 50, 150);
        var usage3 = new GenerationTokenUsage(200, 50, 250);

        // Assert
        usage1.Should().Be(usage2);
        usage1.Should().NotBe(usage3);
    }

    #endregion

    #region EntityImageGenerationResult - Successful Tests

    [Fact]
    public void ImageSuccessful_WithBase64_ShouldCreateResult()
    {
        // Arrange
        var base64Data = "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mNk+M9QDwADhgGAWjR9awAAAABJRU5ErkJggg==";

        // Act
        var result = EntityImageGenerationResult.Successful(base64Data);

        // Assert
        result.Success.Should().BeTrue();
        result.ImageBase64.Should().Be(base64Data);
        result.ImageDataUrl.Should().StartWith("data:image/png;base64,");
        result.ErrorMessage.Should().BeNull();
    }

    [Fact]
    public void ImageSuccessful_WithAllData_ShouldCreateCompleteResult()
    {
        // Arrange
        var base64Data = "base64imagedata";

        // Act
        var result = EntityImageGenerationResult.Successful(
            imageBase64: base64Data,
            storedImageUrl: "https://storage.example.com/images/123.png",
            generatedPrompt: "A wise old wizard in fantasy style");

        // Assert
        result.Success.Should().BeTrue();
        result.ImageBase64.Should().Be(base64Data);
        result.StoredImageUrl.Should().Be("https://storage.example.com/images/123.png");
        result.GeneratedPrompt.Should().Be("A wise old wizard in fantasy style");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void ImageSuccessful_WithEmptyBase64_ShouldThrowArgumentException(string? invalidBase64)
    {
        // Act
        var act = () => EntityImageGenerationResult.Successful(invalidBase64!);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithParameterName("imageBase64");
    }

    #endregion

    #region EntityImageGenerationResult - Failed Tests

    [Fact]
    public void ImageFailed_ShouldCreateFailedResult()
    {
        // Act
        var result = EntityImageGenerationResult.Failed("Image generation API error");

        // Assert
        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Be("Image generation API error");
        result.ImageBase64.Should().BeNull();
        result.ImageDataUrl.Should().BeNull();
        result.StoredImageUrl.Should().BeNull();
        result.GeneratedPrompt.Should().BeNull();
    }

    [Fact]
    public void ImageFailed_ShouldTrimErrorMessage()
    {
        // Act
        var result = EntityImageGenerationResult.Failed("  Error occurred  ");

        // Assert
        result.ErrorMessage.Should().Be("Error occurred");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void ImageFailed_WithEmptyErrorMessage_ShouldThrowArgumentException(string? invalidMessage)
    {
        // Act
        var act = () => EntityImageGenerationResult.Failed(invalidMessage!);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithParameterName("errorMessage");
    }

    #endregion

    #region EntityImageGenerationResult - GetBestUrl Tests

    [Fact]
    public void GetBestUrl_WithStoredUrl_ShouldReturnStoredUrl()
    {
        // Arrange
        var result = EntityImageGenerationResult.Successful(
            imageBase64: "base64data",
            storedImageUrl: "https://storage.example.com/image.png");

        // Act
        var bestUrl = result.GetBestUrl();

        // Assert - stored URL takes precedence
        bestUrl.Should().Be("https://storage.example.com/image.png");
    }

    [Fact]
    public void GetBestUrl_WithoutStoredUrl_ShouldReturnDataUrl()
    {
        // Arrange
        var result = EntityImageGenerationResult.Successful("base64data");

        // Act
        var bestUrl = result.GetBestUrl();

        // Assert - falls back to data URL
        bestUrl.Should().StartWith("data:image/png;base64,");
    }

    [Fact]
    public void GetBestUrl_ForFailedResult_ShouldReturnNull()
    {
        // Arrange
        var result = EntityImageGenerationResult.Failed("Error");

        // Act
        var bestUrl = result.GetBestUrl();

        // Assert
        bestUrl.Should().BeNull();
    }

    #endregion
}
