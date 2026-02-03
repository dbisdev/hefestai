using Loremaster.Domain.ValueObjects;

namespace Loremaster.Tests.Unit.Domain.ValueObjects;

/// <summary>
/// Unit tests for the EntityGenerationConfig value object.
/// Tests creation, validation, and configuration logic (EPIC 4.5 - Entity Assisted Generation).
/// </summary>
public class EntityGenerationConfigTests
{
    private static readonly Guid ValidGameSystemId = Guid.NewGuid();
    private static readonly Guid ValidTemplateId = Guid.NewGuid();
    private const string ValidEntityTypeName = "character";

    #region Create Tests

    [Fact]
    public void Create_WithValidData_ShouldCreateConfig()
    {
        // Arrange & Act
        var config = EntityGenerationConfig.Create(
            gameSystemId: ValidGameSystemId,
            templateId: ValidTemplateId,
            entityTypeName: ValidEntityTypeName,
            userPrompt: "A brave warrior",
            temperature: 0.7f);

        // Assert
        config.GameSystemId.Should().Be(ValidGameSystemId);
        config.TemplateId.Should().Be(ValidTemplateId);
        config.EntityTypeName.Should().Be(ValidEntityTypeName);
        config.UserPrompt.Should().Be("A brave warrior");
        config.Temperature.Should().Be(0.7f);
    }

    [Fact]
    public void Create_ShouldNormalizeEntityTypeName()
    {
        // Arrange & Act
        var config = EntityGenerationConfig.Create(
            gameSystemId: ValidGameSystemId,
            templateId: ValidTemplateId,
            entityTypeName: "  Character  ");

        // Assert - should be trimmed and lowercased
        config.EntityTypeName.Should().Be("character");
    }

    [Fact]
    public void Create_ShouldTrimUserPrompt()
    {
        // Arrange & Act
        var config = EntityGenerationConfig.Create(
            gameSystemId: ValidGameSystemId,
            templateId: ValidTemplateId,
            entityTypeName: ValidEntityTypeName,
            userPrompt: "  A brave warrior  ");

        // Assert
        config.UserPrompt.Should().Be("A brave warrior");
    }

    [Fact]
    public void Create_WithEmptyGameSystemId_ShouldThrowArgumentException()
    {
        // Act
        var act = () => EntityGenerationConfig.Create(
            gameSystemId: Guid.Empty,
            templateId: ValidTemplateId,
            entityTypeName: ValidEntityTypeName);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithParameterName("gameSystemId");
    }

    [Fact]
    public void Create_WithEmptyTemplateId_ShouldThrowArgumentException()
    {
        // Act
        var act = () => EntityGenerationConfig.Create(
            gameSystemId: ValidGameSystemId,
            templateId: Guid.Empty,
            entityTypeName: ValidEntityTypeName);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithParameterName("templateId");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithEmptyEntityTypeName_ShouldThrowArgumentException(string? invalidName)
    {
        // Act
        var act = () => EntityGenerationConfig.Create(
            gameSystemId: ValidGameSystemId,
            templateId: ValidTemplateId,
            entityTypeName: invalidName!);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithParameterName("entityTypeName");
    }

    [Theory]
    [InlineData(-0.1f)]
    [InlineData(1.1f)]
    [InlineData(2.0f)]
    public void Create_WithInvalidTemperature_ShouldThrowArgumentOutOfRangeException(float invalidTemp)
    {
        // Act
        var act = () => EntityGenerationConfig.Create(
            gameSystemId: ValidGameSystemId,
            templateId: ValidTemplateId,
            entityTypeName: ValidEntityTypeName,
            temperature: invalidTemp);

        // Assert
        act.Should().Throw<ArgumentOutOfRangeException>()
            .WithParameterName("temperature");
    }

    [Theory]
    [InlineData(0.0f)]
    [InlineData(0.5f)]
    [InlineData(1.0f)]
    public void Create_WithValidTemperature_ShouldSucceed(float validTemp)
    {
        // Act
        var config = EntityGenerationConfig.Create(
            gameSystemId: ValidGameSystemId,
            templateId: ValidTemplateId,
            entityTypeName: ValidEntityTypeName,
            temperature: validTemp);

        // Assert
        config.Temperature.Should().Be(validTemp);
    }

    #endregion

    #region Default Values Tests

    [Fact]
    public void Create_WithDefaults_ShouldHaveDefaultTemperature()
    {
        // Act
        var config = EntityGenerationConfig.Create(
            gameSystemId: ValidGameSystemId,
            templateId: ValidTemplateId,
            entityTypeName: ValidEntityTypeName);

        // Assert - default temperature is 0.7
        config.Temperature.Should().Be(0.7f);
    }

    [Fact]
    public void Create_WithDefaults_ShouldHaveEmptyFieldsToGenerate()
    {
        // Act
        var config = EntityGenerationConfig.Create(
            gameSystemId: ValidGameSystemId,
            templateId: ValidTemplateId,
            entityTypeName: ValidEntityTypeName);

        // Assert
        config.FieldsToGenerate.Should().BeEmpty();
        config.HasSpecificFields.Should().BeFalse();
    }

    [Fact]
    public void Create_WithDefaults_ShouldHaveEmptyExistingValues()
    {
        // Act
        var config = EntityGenerationConfig.Create(
            gameSystemId: ValidGameSystemId,
            templateId: ValidTemplateId,
            entityTypeName: ValidEntityTypeName);

        // Assert
        config.ExistingValues.Should().BeEmpty();
    }

    [Fact]
    public void Create_WithDefaults_ShouldNotIncludeImageGeneration()
    {
        // Act
        var config = EntityGenerationConfig.Create(
            gameSystemId: ValidGameSystemId,
            templateId: ValidTemplateId,
            entityTypeName: ValidEntityTypeName);

        // Assert
        config.IncludeImageGeneration.Should().BeFalse();
        config.ImageStyle.Should().BeNull();
    }

    #endregion

    #region FieldsToGenerate Tests

    [Fact]
    public void Create_WithFieldsToGenerate_ShouldStoreFields()
    {
        // Arrange
        var fields = new[] { "name", "description", "strength" };

        // Act
        var config = EntityGenerationConfig.Create(
            gameSystemId: ValidGameSystemId,
            templateId: ValidTemplateId,
            entityTypeName: ValidEntityTypeName,
            fieldsToGenerate: fields);

        // Assert
        config.FieldsToGenerate.Should().BeEquivalentTo(fields);
        config.HasSpecificFields.Should().BeTrue();
    }

    [Fact]
    public void ShouldGenerateField_WithNoSpecificFields_ShouldReturnTrueForAll()
    {
        // Arrange
        var config = EntityGenerationConfig.Create(
            gameSystemId: ValidGameSystemId,
            templateId: ValidTemplateId,
            entityTypeName: ValidEntityTypeName);

        // Act & Assert
        config.ShouldGenerateField("anyField").Should().BeTrue();
        config.ShouldGenerateField("anotherField").Should().BeTrue();
    }

    [Fact]
    public void ShouldGenerateField_WithSpecificFields_ShouldReturnTrueOnlyForSpecified()
    {
        // Arrange
        var config = EntityGenerationConfig.Create(
            gameSystemId: ValidGameSystemId,
            templateId: ValidTemplateId,
            entityTypeName: ValidEntityTypeName,
            fieldsToGenerate: new[] { "name", "description" });

        // Act & Assert
        config.ShouldGenerateField("name").Should().BeTrue();
        config.ShouldGenerateField("description").Should().BeTrue();
        config.ShouldGenerateField("strength").Should().BeFalse();
    }

    [Fact]
    public void ShouldGenerateField_ShouldBeCaseInsensitive()
    {
        // Arrange
        var config = EntityGenerationConfig.Create(
            gameSystemId: ValidGameSystemId,
            templateId: ValidTemplateId,
            entityTypeName: ValidEntityTypeName,
            fieldsToGenerate: new[] { "Name", "Description" });

        // Act & Assert
        config.ShouldGenerateField("name").Should().BeTrue();
        config.ShouldGenerateField("NAME").Should().BeTrue();
        config.ShouldGenerateField("description").Should().BeTrue();
    }

    #endregion

    #region ExistingValues Tests

    [Fact]
    public void Create_WithExistingValues_ShouldStoreValues()
    {
        // Arrange
        var existingValues = new Dictionary<string, object?>
        {
            ["name"] = "Gandalf",
            ["level"] = 20
        };

        // Act
        var config = EntityGenerationConfig.Create(
            gameSystemId: ValidGameSystemId,
            templateId: ValidTemplateId,
            entityTypeName: ValidEntityTypeName,
            existingValues: existingValues);

        // Assert
        config.ExistingValues.Should().HaveCount(2);
        config.ExistingValues["name"].Should().Be("Gandalf");
        config.ExistingValues["level"].Should().Be(20);
    }

    [Fact]
    public void GetExistingValue_WithExistingKey_ShouldReturnValue()
    {
        // Arrange
        var config = EntityGenerationConfig.Create(
            gameSystemId: ValidGameSystemId,
            templateId: ValidTemplateId,
            entityTypeName: ValidEntityTypeName,
            existingValues: new Dictionary<string, object?> { ["name"] = "Gandalf" });

        // Act
        var value = config.GetExistingValue("name");

        // Assert
        value.Should().Be("Gandalf");
    }

    [Fact]
    public void GetExistingValue_WithNonExistentKey_ShouldReturnNull()
    {
        // Arrange
        var config = EntityGenerationConfig.Create(
            gameSystemId: ValidGameSystemId,
            templateId: ValidTemplateId,
            entityTypeName: ValidEntityTypeName);

        // Act
        var value = config.GetExistingValue("nonExistent");

        // Assert
        value.Should().BeNull();
    }

    #endregion

    #region ImageGeneration Tests

    [Fact]
    public void Create_WithImageGeneration_ShouldStoreSettings()
    {
        // Act
        var config = EntityGenerationConfig.Create(
            gameSystemId: ValidGameSystemId,
            templateId: ValidTemplateId,
            entityTypeName: ValidEntityTypeName,
            includeImageGeneration: true,
            imageStyle: "fantasy");

        // Assert
        config.IncludeImageGeneration.Should().BeTrue();
        config.ImageStyle.Should().Be("fantasy");
    }

    [Fact]
    public void Create_ShouldTrimImageStyle()
    {
        // Act
        var config = EntityGenerationConfig.Create(
            gameSystemId: ValidGameSystemId,
            templateId: ValidTemplateId,
            entityTypeName: ValidEntityTypeName,
            includeImageGeneration: true,
            imageStyle: "  fantasy  ");

        // Assert
        config.ImageStyle.Should().Be("fantasy");
    }

    #endregion

    #region Equality Tests

    [Fact]
    public void Equals_SameGameSystemTemplateAndEntityType_ShouldBeEqual()
    {
        // Arrange
        var config1 = EntityGenerationConfig.Create(
            gameSystemId: ValidGameSystemId,
            templateId: ValidTemplateId,
            entityTypeName: ValidEntityTypeName);

        var config2 = EntityGenerationConfig.Create(
            gameSystemId: ValidGameSystemId,
            templateId: ValidTemplateId,
            entityTypeName: ValidEntityTypeName);

        // Assert
        config1.Should().Be(config2);
        config1.GetHashCode().Should().Be(config2.GetHashCode());
    }

    [Fact]
    public void Equals_DifferentGameSystemId_ShouldNotBeEqual()
    {
        // Arrange
        var config1 = EntityGenerationConfig.Create(
            gameSystemId: Guid.NewGuid(),
            templateId: ValidTemplateId,
            entityTypeName: ValidEntityTypeName);

        var config2 = EntityGenerationConfig.Create(
            gameSystemId: Guid.NewGuid(),
            templateId: ValidTemplateId,
            entityTypeName: ValidEntityTypeName);

        // Assert
        config1.Should().NotBe(config2);
    }

    [Fact]
    public void Equals_DifferentTemplateId_ShouldNotBeEqual()
    {
        // Arrange
        var config1 = EntityGenerationConfig.Create(
            gameSystemId: ValidGameSystemId,
            templateId: Guid.NewGuid(),
            entityTypeName: ValidEntityTypeName);

        var config2 = EntityGenerationConfig.Create(
            gameSystemId: ValidGameSystemId,
            templateId: Guid.NewGuid(),
            entityTypeName: ValidEntityTypeName);

        // Assert
        config1.Should().NotBe(config2);
    }

    [Fact]
    public void Equals_DifferentEntityTypeName_ShouldNotBeEqual()
    {
        // Arrange
        var config1 = EntityGenerationConfig.Create(
            gameSystemId: ValidGameSystemId,
            templateId: ValidTemplateId,
            entityTypeName: "character");

        var config2 = EntityGenerationConfig.Create(
            gameSystemId: ValidGameSystemId,
            templateId: ValidTemplateId,
            entityTypeName: "vehicle");

        // Assert
        config1.Should().NotBe(config2);
    }

    #endregion
}
