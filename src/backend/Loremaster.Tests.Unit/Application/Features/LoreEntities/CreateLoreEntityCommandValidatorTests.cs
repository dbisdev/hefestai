using FluentValidation.TestHelper;
using Loremaster.Application.Features.LoreEntities.Commands.CreateLoreEntity;
using Loremaster.Domain.Enums;

namespace Loremaster.Tests.Unit.Application.Features.LoreEntities;

/// <summary>
/// Unit tests for CreateLoreEntityCommandValidator.
/// Tests validation rules for entity creation.
/// </summary>
public class CreateLoreEntityCommandValidatorTests
{
    private readonly CreateLoreEntityCommandValidator _validator;
    private readonly Guid _validCampaignId = Guid.NewGuid();

    public CreateLoreEntityCommandValidatorTests()
    {
        _validator = new CreateLoreEntityCommandValidator();
    }

    #region Valid Commands

    [Fact]
    public void Validate_WithValidData_ShouldNotHaveErrors()
    {
        // Arrange
        var command = new CreateLoreEntityCommand(
            CampaignId: _validCampaignId,
            EntityType: "character",
            Name: "Valid Character Name",
            Description: "A valid description");

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_WithMinimalValidData_ShouldNotHaveErrors()
    {
        // Arrange
        var command = new CreateLoreEntityCommand(
            CampaignId: _validCampaignId,
            EntityType: "npc",
            Name: "NPC");

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    #endregion

    #region CampaignId Validation

    [Fact]
    public void Validate_WithEmptyCampaignId_ShouldHaveError()
    {
        // Arrange
        var command = new CreateLoreEntityCommand(
            CampaignId: Guid.Empty,
            EntityType: "character",
            Name: "Test");

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.CampaignId)
            .WithErrorMessage("Campaign ID is required");
    }

    #endregion

    #region EntityType Validation

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_WithEmptyEntityType_ShouldHaveError(string? entityType)
    {
        // Arrange
        var command = new CreateLoreEntityCommand(
            CampaignId: _validCampaignId,
            EntityType: entityType!,
            Name: "Test");

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.EntityType);
    }

    [Theory]
    [InlineData("character")]
    [InlineData("npc")]
    [InlineData("location")]
    [InlineData("item")]
    [InlineData("vehicle")]
    [InlineData("faction")]
    [InlineData("event")]
    [InlineData("creature")]
    [InlineData("quest")]
    [InlineData("note")]
    public void Validate_WithValidEntityType_ShouldNotHaveError(string entityType)
    {
        // Arrange
        var command = new CreateLoreEntityCommand(
            CampaignId: _validCampaignId,
            EntityType: entityType,
            Name: "Test");

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.EntityType);
    }

    [Theory]
    [InlineData("CHARACTER")]
    [InlineData("Character")]
    [InlineData("NPC")]
    [InlineData("Location")]
    public void Validate_WithEntityTypeDifferentCase_ShouldNotHaveError(string entityType)
    {
        // Arrange
        var command = new CreateLoreEntityCommand(
            CampaignId: _validCampaignId,
            EntityType: entityType,
            Name: "Test");

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.EntityType);
    }

    /// <summary>
    /// Tests that entity types with invalid characters are rejected.
    /// Note: The validator now allows any entity type that matches the pattern
    /// (starts with letter, contains only letters, numbers, underscores, hyphens, or spaces).
    /// Specific entity type validation against templates is done at the handler level.
    /// </summary>
    [Theory]
    [InlineData("123invalid")]
    [InlineData("@invalid")]
    [InlineData("#weapon")]
    [InlineData("$spell")]
    [InlineData("!monster")]
    public void Validate_WithInvalidEntityTypePattern_ShouldHaveError(string entityType)
    {
        // Arrange
        var command = new CreateLoreEntityCommand(
            CampaignId: _validCampaignId,
            EntityType: entityType,
            Name: "Test");

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.EntityType)
            .WithErrorMessage("Entity type must start with a letter and contain only letters, numbers, underscores, hyphens, or spaces");
    }

    /// <summary>
    /// Tests that valid entity type patterns are accepted even if not in a predefined list.
    /// Entity type validation against templates is done at the handler level.
    /// </summary>
    [Theory]
    [InlineData("weapon")]
    [InlineData("spell")]
    [InlineData("monster")]
    [InlineData("custom_type")]
    [InlineData("my-entity")]
    public void Validate_WithValidEntityTypePattern_ShouldNotHaveError(string entityType)
    {
        // Arrange
        var command = new CreateLoreEntityCommand(
            CampaignId: _validCampaignId,
            EntityType: entityType,
            Name: "Test");

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.EntityType);
    }

    [Fact]
    public void Validate_WithEntityTypeExceeding50Characters_ShouldHaveError()
    {
        // Arrange
        var longType = new string('a', 51);
        var command = new CreateLoreEntityCommand(
            CampaignId: _validCampaignId,
            EntityType: longType,
            Name: "Test");

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.EntityType)
            .WithErrorMessage("Entity type cannot exceed 50 characters");
    }

    #endregion

    #region Name Validation

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_WithEmptyName_ShouldHaveError(string? name)
    {
        // Arrange
        var command = new CreateLoreEntityCommand(
            CampaignId: _validCampaignId,
            EntityType: "character",
            Name: name!);

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Name)
            .WithErrorMessage("Name is required");
    }

    [Fact]
    public void Validate_WithNameExceeding200Characters_ShouldHaveError()
    {
        // Arrange
        var longName = new string('a', 201);
        var command = new CreateLoreEntityCommand(
            CampaignId: _validCampaignId,
            EntityType: "character",
            Name: longName);

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Name)
            .WithErrorMessage("Name cannot exceed 200 characters");
    }

    [Fact]
    public void Validate_WithNameExactly200Characters_ShouldNotHaveError()
    {
        // Arrange
        var name = new string('a', 200);
        var command = new CreateLoreEntityCommand(
            CampaignId: _validCampaignId,
            EntityType: "character",
            Name: name);

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Name);
    }

    #endregion

    #region Description Validation

    [Fact]
    public void Validate_WithNullDescription_ShouldNotHaveError()
    {
        // Arrange
        var command = new CreateLoreEntityCommand(
            CampaignId: _validCampaignId,
            EntityType: "character",
            Name: "Test",
            Description: null);

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Description);
    }

    [Fact]
    public void Validate_WithDescriptionExceeding5000Characters_ShouldHaveError()
    {
        // Arrange
        var longDescription = new string('a', 5001);
        var command = new CreateLoreEntityCommand(
            CampaignId: _validCampaignId,
            EntityType: "character",
            Name: "Test",
            Description: longDescription);

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Description)
            .WithErrorMessage("Description cannot exceed 5000 characters");
    }

    [Fact]
    public void Validate_WithDescriptionExactly5000Characters_ShouldNotHaveError()
    {
        // Arrange
        var description = new string('a', 5000);
        var command = new CreateLoreEntityCommand(
            CampaignId: _validCampaignId,
            EntityType: "character",
            Name: "Test",
            Description: description);

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Description);
    }

    #endregion

    #region ImageUrl Validation

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void Validate_WithNullOrEmptyImageUrl_ShouldNotHaveError(string? imageUrl)
    {
        // Arrange
        var command = new CreateLoreEntityCommand(
            CampaignId: _validCampaignId,
            EntityType: "character",
            Name: "Test",
            ImageUrl: imageUrl);

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.ImageUrl);
    }

    [Theory]
    [InlineData("https://example.com/image.png")]
    [InlineData("http://example.com/image.jpg")]
    [InlineData("https://cdn.example.com/path/to/image.webp")]
    public void Validate_WithValidImageUrl_ShouldNotHaveError(string imageUrl)
    {
        // Arrange
        var command = new CreateLoreEntityCommand(
            CampaignId: _validCampaignId,
            EntityType: "character",
            Name: "Test",
            ImageUrl: imageUrl);

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.ImageUrl);
    }

    [Theory]
    [InlineData("not-a-url", "Invalid URL format: not-a-url")]
    [InlineData("ftp://example.com/image.png", "URL must use http or https scheme. Got: ftp")]
    [InlineData("example.com/image.png", "Invalid URL format: example.com/image.png")]
    [InlineData("//example.com/image.png", "URL must use http or https scheme. Got: file")]
    public void Validate_WithInvalidImageUrl_ShouldHaveError(string imageUrl, string expectedErrorMessage)
    {
        // Arrange
        var command = new CreateLoreEntityCommand(
            CampaignId: _validCampaignId,
            EntityType: "character",
            Name: "Test",
            ImageUrl: imageUrl);

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.ImageUrl)
            .WithErrorMessage(expectedErrorMessage);
    }

    [Fact]
    public void Validate_WithHttpImageUrlExceeding2000Characters_ShouldHaveError()
    {
        // Arrange
        var longUrl = "https://example.com/" + new string('a', 2000);
        var command = new CreateLoreEntityCommand(
            CampaignId: _validCampaignId,
            EntityType: "character",
            Name: "Test",
            ImageUrl: longUrl);

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.ImageUrl)
            .WithErrorMessage("URL exceeds maximum length of 2000 characters");
    }

    [Fact]
    public void Validate_WithValidBase64DataUri_ShouldNotHaveError()
    {
        // Arrange - Valid base64 data URI for a small PNG image
        var dataUri = "data:image/png;base64,iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mNk+M9QDwADhgGAWjR9awAAAABJRU5ErkJggg==";
        var command = new CreateLoreEntityCommand(
            CampaignId: _validCampaignId,
            EntityType: "character",
            Name: "Test",
            ImageUrl: dataUri);

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.ImageUrl);
    }

    [Theory]
    [InlineData("data:image/jpeg;base64,/9j/4AAQSkZJRg==")]
    [InlineData("data:image/gif;base64,R0lGODlhAQABAIAAAAAAAP///yH5BAEAAAAALAAAAAABAAEAAAIBRAA7")]
    [InlineData("data:image/webp;base64,UklGRh4AAABXRUJQVlA4TBEAAAAvAAAAAAfQ//73v/+BiOh/AAA=")]
    public void Validate_WithValidImageDataUriFormats_ShouldNotHaveError(string dataUri)
    {
        // Arrange
        var command = new CreateLoreEntityCommand(
            CampaignId: _validCampaignId,
            EntityType: "character",
            Name: "Test",
            ImageUrl: dataUri);

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.ImageUrl);
    }

    [Theory]
    [InlineData("data:text/plain;base64,SGVsbG8gV29ybGQ=")] // Not an image type
    [InlineData("data:application/json;base64,eyJ0ZXN0IjogdHJ1ZX0=")] // Not an image type
    public void Validate_WithNonImageDataUri_ShouldHaveError(string dataUri)
    {
        // Arrange
        var command = new CreateLoreEntityCommand(
            CampaignId: _validCampaignId,
            EntityType: "character",
            Name: "Test",
            ImageUrl: dataUri);

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        // The error message includes the truncated data URI for debugging
        result.ShouldHaveValidationErrorFor(x => x.ImageUrl);
        var error = result.Errors.First(e => e.PropertyName == "ImageUrl");
        Assert.StartsWith("Data URI must be an image type (starts with 'data:image/')", error.ErrorMessage);
    }

    [Fact]
    public void Validate_WithDataUriMissingBase64Marker_ShouldHaveError()
    {
        // Arrange - Missing ;base64, marker
        var dataUri = "data:image/png,iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAA";
        var command = new CreateLoreEntityCommand(
            CampaignId: _validCampaignId,
            EntityType: "character",
            Name: "Test",
            ImageUrl: dataUri);

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.ImageUrl)
            .WithErrorMessage("Data URI must contain ';base64,' marker");
    }

    #endregion

    #region Optional Fields

    [Fact]
    public void Validate_WithAllOptionalFields_ShouldNotHaveError()
    {
        // Arrange
        var command = new CreateLoreEntityCommand(
            CampaignId: _validCampaignId,
            EntityType: "character",
            Name: "Full Character",
            Description: "A detailed description",
            OwnershipType: OwnershipType.Player,
            Visibility: VisibilityLevel.Campaign,
            IsTemplate: false,
            ImageUrl: "https://example.com/image.png",
            Attributes: new Dictionary<string, object> { { "level", 5 } },
            Metadata: new Dictionary<string, object> { { "source", "manual" } });

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    #endregion
}
