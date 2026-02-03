using FluentValidation.TestHelper;
using Loremaster.Application.Features.GameSystems.Commands.CreateGameSystem;

namespace Loremaster.Tests.Unit.Application.Features.GameSystems;

/// <summary>
/// Unit tests for CreateGameSystemCommandValidator.
/// Tests validation rules for game system creation.
/// </summary>
public class CreateGameSystemCommandValidatorTests
{
    private readonly CreateGameSystemCommandValidator _validator;

    public CreateGameSystemCommandValidatorTests()
    {
        _validator = new CreateGameSystemCommandValidator();
    }

    #region Valid Commands

    [Fact]
    public void Validate_WithValidData_ShouldNotHaveErrors()
    {
        // Arrange
        var command = new CreateGameSystemCommand(
            Code: "dnd5e",
            Name: "Dungeons & Dragons 5th Edition",
            Publisher: "Wizards of the Coast",
            Version: "5.2",
            Description: "Fantasy roleplaying game");

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_WithMinimalValidData_ShouldNotHaveErrors()
    {
        // Arrange
        var command = new CreateGameSystemCommand(
            Code: "simple",
            Name: "Simple Game");

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    #endregion

    #region Code Validation

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_WithEmptyCode_ShouldHaveError(string? code)
    {
        // Arrange
        var command = new CreateGameSystemCommand(
            Code: code!,
            Name: "Test");

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Code)
            .WithErrorMessage("Code is required");
    }

    [Fact]
    public void Validate_WithCodeExceeding50Characters_ShouldHaveError()
    {
        // Arrange
        var longCode = new string('a', 51);
        var command = new CreateGameSystemCommand(
            Code: longCode,
            Name: "Test");

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Code)
            .WithErrorMessage("Code cannot exceed 50 characters");
    }

    [Theory]
    [InlineData("valid-code")]
    [InlineData("dnd5e")]
    [InlineData("pathfinder-2e")]
    [InlineData("123-abc")]
    [InlineData("a")]
    public void Validate_WithValidCodeFormat_ShouldNotHaveError(string code)
    {
        // Arrange
        var command = new CreateGameSystemCommand(
            Code: code,
            Name: "Test");

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Code);
    }

    [Theory]
    [InlineData("Invalid")]
    [InlineData("UPPERCASE")]
    [InlineData("with space")]
    [InlineData("with_underscore")]
    [InlineData("special!char")]
    [InlineData("dot.dot")]
    public void Validate_WithInvalidCodeFormat_ShouldHaveError(string code)
    {
        // Arrange
        var command = new CreateGameSystemCommand(
            Code: code,
            Name: "Test");

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Code)
            .WithErrorMessage("Code must contain only lowercase letters, numbers, and hyphens");
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
        var command = new CreateGameSystemCommand(
            Code: "valid",
            Name: name!);

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Name)
            .WithErrorMessage("Name is required");
    }

    [Fact]
    public void Validate_WithNameExceeding100Characters_ShouldHaveError()
    {
        // Arrange
        var longName = new string('a', 101);
        var command = new CreateGameSystemCommand(
            Code: "valid",
            Name: longName);

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Name)
            .WithErrorMessage("Name cannot exceed 100 characters");
    }

    [Fact]
    public void Validate_WithNameExactly100Characters_ShouldNotHaveError()
    {
        // Arrange
        var name = new string('a', 100);
        var command = new CreateGameSystemCommand(
            Code: "valid",
            Name: name);

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Name);
    }

    #endregion

    #region Optional Field Validation

    [Fact]
    public void Validate_WithPublisherExceeding100Characters_ShouldHaveError()
    {
        // Arrange
        var longPublisher = new string('a', 101);
        var command = new CreateGameSystemCommand(
            Code: "valid",
            Name: "Test",
            Publisher: longPublisher);

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Publisher)
            .WithErrorMessage("Publisher cannot exceed 100 characters");
    }

    [Fact]
    public void Validate_WithNullPublisher_ShouldNotHaveError()
    {
        // Arrange
        var command = new CreateGameSystemCommand(
            Code: "valid",
            Name: "Test",
            Publisher: null);

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Publisher);
    }

    [Fact]
    public void Validate_WithVersionExceeding50Characters_ShouldHaveError()
    {
        // Arrange
        var longVersion = new string('1', 51);
        var command = new CreateGameSystemCommand(
            Code: "valid",
            Name: "Test",
            Version: longVersion);

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Version)
            .WithErrorMessage("Version cannot exceed 50 characters");
    }

    [Fact]
    public void Validate_WithDescriptionExceeding2000Characters_ShouldHaveError()
    {
        // Arrange
        var longDescription = new string('a', 2001);
        var command = new CreateGameSystemCommand(
            Code: "valid",
            Name: "Test",
            Description: longDescription);

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Description)
            .WithErrorMessage("Description cannot exceed 2000 characters");
    }

    [Fact]
    public void Validate_WithDescriptionExactly2000Characters_ShouldNotHaveError()
    {
        // Arrange
        var description = new string('a', 2000);
        var command = new CreateGameSystemCommand(
            Code: "valid",
            Name: "Test",
            Description: description);

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Description);
    }

    #endregion
}
